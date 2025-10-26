using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// A formatter that can serialize an entire fixed-size span with one call.
/// This is primarily used as an optimization with <see cref="BlitFormatter{T}"/>.
/// </summary>
/// <typeparam name="T">The element type of the span.</typeparam>
internal interface ISpanFormatter<T>
{
    /// <summary>
    /// Deserializes the contents of an array in order, writing the output to <paramref name="elements"/>.
    /// </summary>
    /// <param name="reader">The input data.</param>
    /// <param name="elements">The output where elements should be stored.</param>
    void Deserialize(BufferReader reader, Span<T> elements);

    /// <summary>
    /// Serializes the contents of an array in order, reading the data from <paramref name="elements"/>.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="elements">The elements to write.</param>
    void Serialize(BufferWriter writer, ReadOnlySpan<T> elements);
}
