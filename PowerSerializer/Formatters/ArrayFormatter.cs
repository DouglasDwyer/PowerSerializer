using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes an array element-by-element.
/// </summary>
/// <typeparam name="T">The element type of the array.</typeparam>
/// <typeparam name="A">The array type itself.</typeparam>
internal sealed class ArrayFormatter<T, A> : ArrayFormatterBase<T, A> where A : notnull
{
    /// <summary>
    /// The formatter to use for individual array elements.
    /// </summary>
    private readonly IFormatter<T> _elementFormatter;

    /// <inheritdoc/>
    protected override void DeserializeElements(BufferReader reader, Span<T> elements)
    {
        for (var i = 0; i < elements.Length; i++)
        {
            _elementFormatter.Deserialize(reader, out elements[i]);
        }
    }

    /// <inheritdoc/>
    protected override void SerializeElements(BufferWriter writer, Span<T> elements)
    {
        for (var i = 0; i < elements.Length; i++)
        {
            _elementFormatter.Serialize(writer, elements[i]);
        }
    }
}
