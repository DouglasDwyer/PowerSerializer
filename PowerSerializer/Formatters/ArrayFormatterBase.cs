using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Implements base functionality for array serialization - namely, the encoding/decoding of array lengths and bounds.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
/// <typeparam name="A">The array type itself.</typeparam>
internal abstract class ArrayFormatterBase<T, A> : IFormatter<A> where A : notnull
{
    /// <summary>
    /// Constructs a new array formatter.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="A"/> is not an array or <typeparamref name="T"/> is not its element type.
    /// These invariants are checked at runtime, because they are impossible to encode in C#'s type system.
    /// </exception>
    public ArrayFormatterBase()
    {
        if (!typeof(A).IsArray)
        {
            throw new ArgumentException($"Generic parameter {typeof(A)} did not correspond to array type", nameof(A));
        }

        if (typeof(A).GetElementType() != typeof(T))
        {
            throw new ArgumentException($"Generic parameter {typeof(T)} did not match expected array element type {typeof(A)}", nameof(T));
        }
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

            for (var i = 0; i < typeof(A).GetArrayRank(); i++)
            {
                lengths[i] = (int)reader.ReadVarUInt32();
            }

            if (typeof(A).IsVariableBoundArray)
            {
                var lowerBounds = new int[typeof(A).GetArrayRank()];
                for (var i = 0; i < typeof(A).GetArrayRank(); i++)
                {
                    lowerBounds[i] = (int)reader.ReadVarUInt32();
                }

                value = (A)(object)Array.CreateInstanceFromArrayType(typeof(A), lengths, lowerBounds);
            }
            else
            {
                value = (A)(object)Array.CreateInstanceFromArrayType(typeof(A), lengths);
            }
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

        if (typeof(A).IsVariableBoundArray)
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
    protected abstract void DeserializeElements(BufferReader reader, Span<T> elements);

    /// <summary>
    /// Serializes the contents of an array in order, reading the data from <paramref name="elements"/>.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="elements">The elements to write.</param>
    protected abstract void SerializeElements(BufferWriter writer, Span<T> elements);

    /// <summary>
    /// Gets a span over all elements of the array.
    /// </summary>
    /// <param name="value">The array to examine.</param>
    /// <returns>A span over all elements.</returns>
    /// <exception cref="ArgumentException">If the array did not have concrete type <c>A</c>.</exception>
    private Span<T> GetSpan(Array value)
    {
        unsafe
        {
            if (value.GetType() != typeof(A))
            {
                throw new ArgumentException("Cannot get span for covariant array value", nameof(value));
            }

            return new Span<T>(Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(value)), value.Length);
        }
    }
}
