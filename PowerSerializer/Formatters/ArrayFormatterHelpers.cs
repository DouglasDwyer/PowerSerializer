using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Implements base functionality for array serialization - namely, the encoding/decoding of array lengths and bounds.
/// </summary>
internal static class ArrayFormatterHelpers
{
    /// <summary>
    /// Instantiates an array formatter, specialized to the element type of <typeparamref name="A"/>.
    /// </summary>
    /// <remarks>
    /// Due to the limitations of C#'s type system, it is impossible to write "the element type of <typeparamref name="A"/>"
    /// as a type within generic code. However, having element type as a concrete generic parameter is necessary for array
    /// serialization to work. As a workaround, this method instantiates a formatter with both generic parameters:
    /// the element type and the array type.
    /// </remarks>
    /// <typeparam name="A">The array type itself.</typeparam>
    /// <param name="serializer">The serializer for which this type is being constructed.</param>
    /// <param name="concreteFormatterDefinition"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="A"/> was not an array or <paramref name="concreteFormatterDefinition"/>
    /// was not a generic type definition with two parameters.
    /// </exception>
    public static IFormatter<A> GetConcreteFormatter<A>(PowerSerializer serializer, Type concreteFormatterDefinition)
    {
        if (!typeof(A).IsArray)
        {
            throw new ArgumentException("Expected array type", nameof(A));
        }

        if (concreteFormatterDefinition.GetGenericArguments().Length != 1)
        {
            throw new ArgumentException("Expected concrete formatter type to have one type arguments - the element type");
        }

        var concreteFormatterType = concreteFormatterDefinition.MakeGenericType([typeof(A).GetElementType()!]);

        if (concreteFormatterType.GetConstructor([typeof(PowerSerializer)]) is null)
        {
            return (IFormatter<A>)Activator.CreateInstance(concreteFormatterType)!;
        }
        else
        {
            return (IFormatter<A>)Activator.CreateInstance(concreteFormatterType, serializer)!;
        }
    }

    /// <summary>
    /// The concrete implementation for the array formatter - specialized to its element type.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    /// <typeparam name="A">The array type itself.</typeparam>
    internal abstract class ConcreteFormatterBase<T, A> : IFormatter<A> where A : notnull
    {
        /// <summary>
        /// Constructs a new array formatter.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If <typeparamref name="A"/> is not an array or <typeparamref name="T"/> is not its element type.
        /// These invariants are checked at runtime, because they are impossible to encode in C#'s type system.
        /// </exception>
        public ConcreteFormatterBase()
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
        protected abstract void DeserializeElements(BufferReader reader, Span<T?> elements);

        /// <summary>
        /// Serializes the contents of an array in order, reading the data from <paramref name="elements"/>.
        /// </summary>
        /// <param name="writer">The output buffer.</param>
        /// <param name="elements">The elements to write.</param>
        protected abstract void SerializeElements(BufferWriter writer, Span<T?> elements);

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
}