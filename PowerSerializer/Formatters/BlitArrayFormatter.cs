using System;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes an array by copying the underlying memory verbatim.
/// The element type of <typeparamref name="A"/> must be blittable.
/// </summary>
/// <typeparam name="A">The array type itself.</typeparam>
public sealed class BlitArrayFormatter<A> : IFormatter<A> where A : notnull
{
    /// <summary>
    /// The specialized formatter type to use.
    /// </summary>
    private readonly IFormatter<A> _concreteFormatter;

    /// <summary>
    /// Creates a new array formatter.
    /// </summary>
    /// <param name="serializer">The serializer that will use this format.</param>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="A"/> was not a valid array type.
    /// </exception>
    public BlitArrayFormatter(PowerSerializer serializer)
    {
        // todo: assert that A's element type is blittable

        _concreteFormatter = ArrayFormatterHelpers.GetConcreteFormatter<A>(serializer, typeof(ConcreteFormatter<>));
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        _concreteFormatter.Deserialize(reader, out value);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        _concreteFormatter.Serialize(writer, value);
    }

    /// <summary>
    /// Specialized formatter implementation.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    internal class ConcreteFormatter<T> : ArrayFormatterHelpers.ConcreteFormatterBase<T, A> where T : unmanaged
    {
        /// <inheritdoc/>
        protected override void DeserializeElements(BufferReader reader, Span<T> elements)
        {
            var resultBytes = MemoryMarshal.AsBytes(elements);
            reader.Read(resultBytes.Length).CopyTo(resultBytes);
        }

        /// <inheritdoc/>
        protected override void SerializeElements(BufferWriter writer, Span<T> elements)
        {
            writer.Write(MemoryMarshal.AsBytes(elements));
        }
    }
}
