using System.Buffers;
using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Formats <see cref="Stack{T}"/> objects.
/// </summary>
/// <typeparam name="T">The stack element type.</typeparam>
public sealed class StackFormatter<T> : IFormatter<Stack<T?>>
{
    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public StackFormatter(PowerSerializer serializer)
    {
        _elementFormatter = serializer.GetFormatter<T>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Stack<T?> value)
    {
        var count = (int)reader.ReadVarUInt32();

        value = new Stack<T?>(count);

        var array = ArrayPool<T?>.Shared.Rent(count);
        for (int i = 0; i < count; i++)
        {
            _elementFormatter.Deserialize(reader, out array[i]);
        }

        // Push in reverse order to restore the original stack
        for (int i = count - 1; 0 <= i; i--)
        {
            value.Push(array[i]);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Stack<T?> value)
    {
        writer.WriteVarUInt32((uint)value.Count);

        foreach (var element in value)
        {
            _elementFormatter.Serialize(writer, element);
        }
    }
}
