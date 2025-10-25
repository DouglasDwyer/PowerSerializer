using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer;

// todo: better name, this one conflicts with namespace

public sealed class PowerSerializer
{
    /// <summary>
    /// A handle to the generic <see cref="CreateFormatter{T}"/> method.
    /// </summary>
    private static MethodInfo CreateFormatterMethod = typeof(PowerSerializer).GetMethod("CreateFormatter", BindingFlags.NonPublic | BindingFlags.Instance)!;

    // todo: prevent mutation :(
    /// <summary>
    /// The options that this serializer will use.
    /// </summary>
    public readonly PowerSerializerOptions Options;

    private readonly FormatterList _formatterList;

    private readonly ConditionalWeakTable<Type, PolymorphicDispatcher> _dispatchers;

    /// <summary>
    /// Locked when the serializer is generating new formatters.
    /// </summary>
    private readonly object _formatterLocker;

    /// <summary>
    /// This helps to support <b>formatters</b> that require cyclic references to themselves.
    /// Whenever a value formatter is created, if a <see cref="SealedReferenceFormatter{T}"/> is recursively
    /// requested for it, it is added here. The reference formatter will be initialized <b>after</b>
    /// the value formatter.
    /// </summary>
    private readonly Dictionary<Type, ISealedReferenceFormatter?> _incompleteReferenceFormatters;

    /// <summary>
    /// The <see cref="SealedReferenceFormatter{T}"/> or <see cref="PolymorphicReferenceFormatter{T}"/>
    /// for a C# reference type.
    /// </summary>
    private readonly Dictionary<Type, object> _referenceformatters;

    /// <summary>
    /// The "original" formatter for a type. Defines how to serialize/deserialize the object
    /// contents (i.e. the object's value). This list stores both formatters for both
    /// C# reference and value types, though.
    /// </summary>
    private readonly Dictionary<Type, object> _valueFormatters;

    public PowerSerializer(PowerSerializerOptions options)
    {
        _formatterList = FormatterList.Default;  // todo
        _formatterLocker = new object();
        _dispatchers = new ConditionalWeakTable<Type, PolymorphicDispatcher>();
        _incompleteReferenceFormatters = new Dictionary<Type, ISealedReferenceFormatter?>();
        Options = options;
        _referenceformatters = new Dictionary<Type, object>();
        _valueFormatters = new Dictionary<Type, object>();
    }

    /// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T)"/>
    public ArraySegment<byte> Serialize<T>(in T value)
    {
        var writer = new ArrayBufferWriter<byte>();
        Serialize(writer, value);
        MemoryMarshal.TryGetArray(writer.WrittenMemory, out var result);
        return result;
    }

    /// <summary>
    /// Converts the provided value to binary data.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="writer">Where the output data should be written.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// An array segment containing the serialized data.
    /// </returns>
    public void Serialize<T>(IBufferWriter<byte> writer, in T? value)
    {
        var context = SerializationContext.Pool.Get();
        try
        {
            GetFormatter<T>().Serialize(new BufferWriter(context, writer), value);
        }
        finally
        {
            SerializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer must contain <b>exactly</b>
    /// the data for <typeparamref name="T"/>, and nothing else. To deserialize only a portion of a buffer,
    /// use the <see cref="Deserialize{T}(ref ReadOnlySpan{byte})"/> overload.
    /// </param>
    /// <returns>The generated object.</returns>
    /// <exception cref="InvalidDataException">
    /// If there was leftover data in the buffer after serialization.
    /// </exception>
    public T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        var result = Deserialize<T>(ref data);
        
        if (0 < data.Length)
        {
            throw new InvalidDataException("Deserialization did not consume all bytes in the provided data");
        }

        return result;
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer may be <b>larger</b>
    /// than the data for <typeparamref name="T"/>. It will be replaced with a span
    /// containing whatever data remained after deserialization.
    /// </param>
    /// <returns>The generated object.</returns>
    public T? Deserialize<T>(ref ReadOnlySpan<byte> data)
    {
        var context = DeserializationContext.Pool.Get();
        try
        {
            var position = 0;
            GetFormatter<T>().Deserialize(new BufferReader(context, data, ref position), out var result);
            data = data[position..];
            return result;
        }
        finally
        {
            DeserializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Gets the formatter to use when serializing objects with base type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The base class of all objects to be serialized.
    /// </typeparam>
    /// <returns>
    /// The formatter to use.
    /// </returns>
    public IFormatter<T?> GetFormatter<T>()
    {
        if (typeof(T).IsValueType)
        {
            return (IFormatter<T?>)GetValueFormatter(typeof(T));
        }
        else
        {
            return (IFormatter<T?>)GetReferenceFormatter(typeof(T));
        }
    }

    /// <summary>
    /// Gets a type-erased dispatcher for serializing/deserializing
    /// objects whose true type is <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The actual, concrete object type.</param>
    /// <returns>
    /// A dispatcher that can be used to serialize and deserialize it.
    /// </returns>
    internal PolymorphicDispatcher GetPolymorphicDispatcher(Type type)
    {
        return _dispatchers.GetValue(type, CreatePolymorphicDispatcher);
    }

    private object GetReferenceFormatter(Type type)
    {
        lock (_formatterLocker)
        {
            if (_referenceformatters.TryGetValue(type, out var formatter))
            {
                return formatter;
            }

            if (IsPolymorphic(type))
            {
                var newFormatter = Activator.CreateInstance(typeof(PolymorphicReferenceFormatter<>).MakeGenericType(type), [this])!;
                _referenceformatters.Add(type, newFormatter);
                return newFormatter;
            }
            else
            {
                ref var incompleteFormatter = ref CollectionsMarshal.GetValueRefOrNullRef(_incompleteReferenceFormatters, type);

                if (Unsafe.IsNullRef(ref incompleteFormatter))
                {
                    var valueFormatter = GetValueFormatter(type);

                    // After generating value formatter, check again
                    // The sealed reference formatter may have been recursively generated
                    if (_referenceformatters.TryGetValue(type, out formatter))
                    {
                        return formatter;
                    }
                    else
                    {
                        var newFormatter = (ISealedReferenceFormatter)Activator.CreateInstance(
                            typeof(SealedReferenceFormatter<>).MakeGenericType(type))!;
                        newFormatter.SetValueFormatter(valueFormatter);
                        return newFormatter;
                    }
                }
                else
                {
                    // Register incomplete formatter to be filled when the value
                    // formatter is fully generated
                    if (incompleteFormatter is null)
                    {
                        incompleteFormatter = (ISealedReferenceFormatter)Activator.CreateInstance(
                            typeof(SealedReferenceFormatter<>).MakeGenericType(type))!;
                    }

                    return incompleteFormatter;
                }
            }
        }
    }

    private object GetValueFormatter(Type type)
    {
        lock (_formatterLocker)
        {
            if (_valueFormatters.TryGetValue(type, out var formatter))
            {
                return formatter;
            }

            _incompleteReferenceFormatters.Add(type, null);

            formatter = CreateFormatterMethod.MakeGenericMethod(type).Invoke(this, null)!;
            _valueFormatters.Add(type, formatter);

            if (_incompleteReferenceFormatters.Remove(type, out var referenceFormatter))
            {
                referenceFormatter?.SetValueFormatter(formatter);
            }

            return formatter;
        }
    }

    /// <summary>
    /// Creates the polymorphic dispatcher to use when serializing/deserializing
    /// objects whose true type is <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The actual, concrete object type.</param>
    /// <returns>
    /// The dispatcher that was generated.
    /// </returns>
    private PolymorphicDispatcher CreatePolymorphicDispatcher(Type type)
    {
        return PolymorphicDispatcher.Create(type, GetValueFormatter(type));
    }

    private IFormatter<T?> CreateFormatter<T>()
    {
        // todo: i hate this code
        foreach (var entry in _formatterList.Entries)
        {
            var equation = new GenericEquation(entry.FormatterType.GetGenericArguments());
            foreach (var iface in entry.FormatterType.GetInterfaces())
            {
                if (equation.Solve(typeof(IFormatter<T?>), iface, out var substitutions))
                {
                    Type type;
                    try
                    {
                        type = entry.FormatterType.IsGenericType ? entry.FormatterType.MakeGenericType(substitutions) : entry.FormatterType;
                    }
                    catch { continue; }

                    foreach (var args in new[] { new[] { this }.Concat(entry.ConstructorArguments).ToArray(), entry.ConstructorArguments.ToArray() })
                    {
                        try
                        {
                            var args2 = args;
                            var methodBase = Type.DefaultBinder.BindToMethod(
                                BindingFlags.CreateInstance,
                                type.GetConstructors(),
                                ref args2!,
                                null,
                                null,
                                null,
                                out _
                            );

                            var result = ((ConstructorInfo)methodBase).Invoke(args);
                            return (IFormatter<T?>)result;
                        }
                        catch { continue; }
                    }
                }
            }
        }

        throw new MissingFormatterException(typeof(T));
    }

    /// <summary>
    /// Determines whether references to objects of <paramref name="type"/>
    /// require the <see cref="PolymorphicReferenceFormatter{T}"/>.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns></returns>
    private static bool IsPolymorphic(Type type)
    {
        return (!type.IsSealed || type.IsArray)
            && !type.IsAssignableTo(typeof(Assembly))
            && !(type != typeof(MemberInfo) && type.IsAssignableTo(typeof(MemberInfo)));
    }
}