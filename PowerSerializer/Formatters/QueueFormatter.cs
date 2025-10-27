using System.Buffers;
using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Formats <see cref="Queue{T}"/> objects.
/// </summary>
/// <typeparam name="T">The queue element type.</typeparam>
public sealed class QueueFormatter<T> : IFormatter<Queue<T?>>
{
    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public QueueFormatter(PowerSerializer serializer)
    {
        _elementFormatter = serializer.GetFormatter<T>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Queue<T?> value)
    {
        var count = (int)reader.ReadVarUInt32();

        value = new Queue<T?>(count);

        for (int i = 0; i < count; i++)
        {
            _elementFormatter.Deserialize(reader, out var element);
            value.Enqueue(element);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Queue<T?> value)
    {
        writer.WriteVarUInt32((uint)value.Count);

        foreach (var element in value)
        {
            _elementFormatter.Serialize(writer, element);
        }
    }
}
