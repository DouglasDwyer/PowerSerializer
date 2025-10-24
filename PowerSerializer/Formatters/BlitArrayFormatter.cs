using System;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes an array by copying the underlying memory verbatim.
/// The element type <typeparamref name="T"/> must be blittable.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
/// <typeparam name="A">The array type itself.</typeparam>
internal class BlitArrayFormatter<T, A> : ArrayFormatterBase<T, A> where A : notnull where T : unmanaged
{
    public BlitArrayFormatter()
    {
    }

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
