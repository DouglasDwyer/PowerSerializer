using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer;

// todo: better name, this one conflicts with namespace

public sealed class PowerSerializer
{
    private readonly FormatterList _formatterList;

    private readonly ConditionalWeakTable<Type, PolymorphicDispatcher> _dispatchers;

    private readonly ConditionalWeakTable<Type, FormatterSet> _formatterSets;

    public PowerSerializer()
    {
        _formatterList = FormatterList.Default;  // todo
        _dispatchers = new ConditionalWeakTable<Type, PolymorphicDispatcher>();
        _formatterSets = new ConditionalWeakTable<Type, FormatterSet>();
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

    public IFormatter<T?> GetFormatter<T>()
    {
        if (_formatterSets.TryGetValue(typeof(T), out var set))
        {
            return (IFormatter<T?>)set.Formatter;
        }
        else
        {
            return (IFormatter<T?>)CreateFormatterSet<T>().Formatter;
        }
        /*
        if (_formatters.TryGetValue(typeof(T), out var formatterSet))
        {
            return formatterSet.GetFormatter<T>();
        }
        else
        {
            // Create reference formatter (if possible)
            // go back and initialize the reference formatter after

            foreach (var entry in _formatterList.Entries)
            {
                var equation = new GenericEquation(entry.FormatterType.GetGenericArguments());
                foreach (var iface in entry.FormatterType.GetInterfaces())
                {
                    if (equation.Solve(typeof(IFormatter<T>), iface, out var substitutions))
                    {
                        try
                        {
                            var concreteType = entry.FormatterType.IsGenericType ? entry.FormatterType.MakeGenericType(substitutions) : entry.FormatterType;
                            var formatter = entry.Construct(this);

                            // temporarily add formatter to lists..?

                        }
                        catch
                        {
                            // Substitution failed (perhaps due to a generic parameter constraint or constructor exception)
                            // todo: remove formatter if failed.
                        }
                    }
                }
            }

            // Create and init polymorphic formatter
        }
        // check _formatters and return
        // go through each type in _formatterList
        // if generics solvable
        //   if type CONCRETE and we already have it in the concrete dictionary, cast and ADD it
        //   else try construct formatter - if success, add it.
        //   otherwise, move next formatter
        // if value type, return formatter
        // if reference type, return reference formatter (which may be polymorphic for non-sealed types)

        // need to store 1 formatter: the thing returned from this (+ the ty-erased formatter for polymorphic scenarios)
        throw new NotImplementedException();*/
    }

    internal PolymorphicDispatcher GetPolymorphicDispatcher(Type type)
    {
        if (_dispatchers.TryGetValue(type, out var dispatcher))
        {
            return dispatcher;
        }
        else
        {
            dispatcher = PolymorphicDispatcher.Create(type, GetFormatterSet(type).ValueFormatter);
            _dispatchers.TryAdd(type, dispatcher);
            return dispatcher;
        }
    }

    private FormatterSet GetFormatterSet(Type type)
    {
        if (_formatterSets.TryGetValue(type, out var result))
        {
            return result;
        }
        else
        {
            var formatterSet = typeof(PowerSerializer).GetMethod("GetFormatterSet", BindingFlags.NonPublic | BindingFlags.Instance, Array.Empty<Type>())!
                .MakeGenericMethod(type).Invoke(this, Array.Empty<object>());  // todo: cache the method
        }
    }

    private FormatterSet CreateFormatterSet<T>()
    {
    }

    private IFormatter<T?> CreateFormatter<T>()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Determines whether references to objects of <paramref name="type"/>
    /// require the <see cref="PolymorphicReferenceFormatter{T}"/>.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns></returns>
    private static bool IsPolymorphic(Type type)
    {
        return !type.IsValueType
            && !type.IsSealed
            && !type.IsAssignableTo(typeof(Assembly))
            && !(type != typeof(MemberInfo) && type.IsAssignableTo(typeof(MemberInfo)));
    }

    private sealed class FormatterSet
    {
        /// <summary>
        /// The formatter to return from <see cref="GetFormatter{T}"/>. For value types,
        /// this should be the original value serializer. For reference types, this should
        /// be either a <see cref="SealedReferenceFormatter{T}"/> or <see cref="PolymorphicReferenceFormatter{T}"/>.
        /// </summary>
        public required object Formatter;

        /// <summary>
        /// The original value serializer, which can encode/decode the type's contents.
        /// </summary>
        public required object ValueFormatter;
    }
}