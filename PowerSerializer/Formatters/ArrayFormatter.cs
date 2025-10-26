using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes an array element-by-element.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
/// <typeparam name="A">The array type itself.</typeparam>
public sealed class ArrayFormatter<T, A> : IFormatter<A> where A : notnull
{
    /// <summary>
    /// Formatter for serializing array elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// A formatter that can serialize an entire fixed-size span with one call.
    /// This is primarily used as an optimization with <see cref="BlitFormatter{T}"/>.
    /// </summary>
    private readonly ISpanFormatter<T?>? _spanFormatter;

    /// <summary>
    /// Constructs a new array formatter.
    /// </summary>
    /// <param name="serializer">The serializer associated with this formatter.</param>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="A"/> is not an array or <typeparamref name="T"/> is not its element type.
    /// These invariants are checked at runtime, because they are impossible to encode in C#'s type system.
    /// </exception>
    public ArrayFormatter(PowerSerializer serializer)
    {
        if (!typeof(A).IsArray)
        {
            throw new ArgumentException($"Generic parameter {typeof(A)} did not correspond to array type", nameof(A));
        }

        if (typeof(A).GetElementType() != typeof(T))
        {
            throw new ArgumentException($"Generic parameter {typeof(T)} did not match expected array element type {typeof(A)}", nameof(T));
        }

        _elementFormatter = serializer.GetFormatter<T>();
        _spanFormatter = _elementFormatter as ISpanFormatter<T?>;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        if (typeof(A).IsSZArray)
        {
            value = (A)(object)new T[reader.ReadVarUInt32()];
        }
        else
        {
            var lengths = new int[typeof(A).GetArrayRank()];
            var lowerBounds = new int[typeof(A).GetArrayRank()];

            for (var i = 0; i < typeof(A).GetArrayRank(); i++)
            {
                lengths[i] = (int)reader.ReadVarUInt32();
            }

            for (var i = 0; i < typeof(A).GetArrayRank(); i++)
            {
                lowerBounds[i] = (int)reader.ReadVarUInt32();
            }

            value = (A)(object)Array.CreateInstanceFromArrayType(typeof(A), lengths, lowerBounds);
        }

        DeserializeElements(reader, GetSpan((Array)(object)value));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        var array = (Array)(object)value;

        for (var i = 0; i < typeof(A).GetArrayRank(); i++)
        {
            writer.WriteVarUInt32((uint)array.GetLength(i));
        }

        if (!typeof(A).IsSZArray)
        {
            for (var i = 0; i < typeof(A).GetArrayRank(); i++)
            {
                writer.WriteVarUInt32((uint)array.GetLowerBound(i));
            }
        }

        SerializeElements(writer, GetSpan(array));
    }

    /// <summary>
    /// Deserializes the contents of an array in order, writing the output to <paramref name="elements"/>.
    /// </summary>
    /// <param name="reader">The input data.</param>
    /// <param name="elements">The output where elements should be stored.</param>
    private void DeserializeElements(BufferReader reader, Span<T?> elements)
    {
        if (_spanFormatter is null)
        {
            for (var i = 0; i < elements.Length; i++)
            {
                _elementFormatter.Deserialize(reader, out elements[i]);
            }
        }
        else
        {
            _spanFormatter.Deserialize(reader, elements);
        }
    }

    /// <summary>
    /// Serializes the contents of an array in order, reading the data from <paramref name="elements"/>.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="elements">The elements to write.</param>
    private void SerializeElements(BufferWriter writer, Span<T?> elements)
    {
        if (_spanFormatter is null)
        {
            for (var i = 0; i < elements.Length; i++)
            {
                _elementFormatter.Serialize(writer, elements[i]);
            }
        }
        else
        {
            _spanFormatter.Serialize(writer, elements);
        }
    }

    /// <summary>
    /// Gets a span over all elements of the array.
    /// </summary>
    /// <param name="value">The array to examine.</param>
    /// <returns>A span over all elements.</returns>
    /// <exception cref="ArgumentException">If the array did not have concrete type <c>A</c>.</exception>
    private Span<T?> GetSpan(Array value)
    {
        if (value.GetType() != typeof(A))
        {
            throw new ArgumentException("Cannot get span for covariant array value", nameof(value));
        }

        return MemoryMarshal.CreateSpan(ref Unsafe.As<byte, T?>(ref MemoryMarshal.GetArrayDataReference(value)), value.Length);
    }
}