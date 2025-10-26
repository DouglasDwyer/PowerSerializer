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
    private static MethodInfo CreateFormatterMethod = typeof(PowerSerializer).GetMethod(nameof(CreateFormatter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    // todo: prevent mutation :(
    /// <summary>
    /// The options that this serializer will use.
    /// </summary>
    public readonly PowerSerializerOptions Options;

    private readonly FormatterList _formatterList;

    private readonly ConditionalWeakTable<Type, ContentFormatters> _contentFormatters;

    private readonly ConditionalWeakTable<Type, object> _referenceFormatters;

    public PowerSerializer() : this(new PowerSerializerOptions()) { }

    public PowerSerializer(PowerSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new PlatformNotSupportedException("PowerSerializer requires runtime support for dynamic code generation");
        }

        _formatterList = FormatterList.Default;  // todo
        _referenceFormatters = new ConditionalWeakTable<Type, object>();
        _contentFormatters = new ConditionalWeakTable<Type, ContentFormatters>();
        Options = options;
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
    /// Gets the formatter to use when serializing objects with base type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The base class of all objects to be serialized.
    /// </param>
    /// <returns>
    /// The formatter to use. This can be cast to <see cref="IFormatter{T}"/> where <c>T</c> equals <paramref name="type"/>.
    /// </returns>
    public object GetFormatter(Type type)
    {
        if (type.IsValueType)
        {
            return _contentFormatters.GetValue(type, CreateContentFormatters).ContentFormatter;
        }
        else
        {
            return _referenceFormatters.GetValue(type, CreateReferenceFormatter);
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
        return (IFormatter<T?>)GetFormatter(typeof(T));
    }

    /// <summary>
    /// Gets a type-erased dispatcher for serializing/deserializing
    /// objects whose true type is <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The actual, concrete object type.</param>
    /// <returns>
    /// A dispatcher that can be used to serialize and deserialize it.
    /// </returns>
    internal IFormatter<object> GetPolymorphicDispatcher(Type type)
    {
        return _contentFormatters.GetValue(type, CreateContentFormatters).PolymorphicDispatcher;
    }


    private ContentFormatters CreateContentFormatters(Type type)
    {
        var contentFormatter = CreateFormatterMethod.MakeGenericMethod(type).Invoke(this, null)!;
        var polymorphicDispatcher = PolymorphicDispatcher.Create(type, contentFormatter);
        return new ContentFormatters { ContentFormatter = contentFormatter, PolymorphicDispatcher = polymorphicDispatcher };
    }

    private object CreateReferenceFormatter(Type type)
    {
        return Activator.CreateInstance(typeof(ReferenceFormatter<>).MakeGenericType(type), [this])!;
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

    private class ContentFormatters
    {
        public required object ContentFormatter;
        public required IFormatter<object> PolymorphicDispatcher;
    }
}