using System;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

internal class UnmanagedArrayFormatter<T, A> : ArrayFormatterBase<T, A> where A : notnull where T : unmanaged
{
    public UnmanagedArrayFormatter()
    {
        // todo: assert that T is ACTUALLY blittable (i.e. unmanaged, no booleans, little endian)
        // tbh would also want to check that the fields are publicly constructible:
        // i.e. this should be an optimization for DynamicFormatter and not enable serialization of additional types.
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
