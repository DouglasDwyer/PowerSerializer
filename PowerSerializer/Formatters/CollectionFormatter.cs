using System;
using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes generic collection types.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <typeparam name="A">The type of the collection itself.</typeparam>
public sealed class CollectionFormatter<T, A> : IFormatter<A> where A : ICollection<T?>, new()
{
    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public CollectionFormatter(PowerSerializer serializer)
    {
        _elementFormatter = serializer.GetFormatter<T>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        var count = (int)reader.ReadVarUInt32();

        value = new A();

        for (var i = 0; i < count; i++)
        {
            _elementFormatter.Deserialize(reader, out var element);
            value.Add(element);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        writer.WriteVarUInt32((uint)value.Count);

        foreach (var element in value)
        {
            _elementFormatter.Serialize(writer, element);
        }
    }
}
