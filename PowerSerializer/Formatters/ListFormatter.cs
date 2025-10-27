using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Formats <see cref="List{T}"/> objects.
/// </summary>
/// <typeparam name="T">The list element type.</typeparam>
public sealed class ListFormatter<T> : IFormatter<List<T?>>
{
    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    /// <summary>
    /// A formatter that can serialize an entire fixed-size span with one call.
    /// This is primarily used as an optimization with <see cref="BlitFormatter{T}"/>.
    /// </summary>
    private readonly ISpanFormatter<T?>? _spanFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    public ListFormatter(PowerSerializer serializer)
    {
        _elementFormatter = serializer.GetFormatter<T>();
        _spanFormatter = _elementFormatter as ISpanFormatter<T?>;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out List<T?> value)
    {
        var count = (int)reader.ReadVarUInt32();

        value = new List<T?>(count);
        CollectionsMarshal.SetCount(value, count);

        var span = CollectionsMarshal.AsSpan(value);
        if (_spanFormatter is null)
        {
            for (var i = 0; i < value.Count; i++)
            {
                _elementFormatter.Deserialize(reader, out span[i]);
            }
        }
        else
        {
            _spanFormatter.Deserialize(reader, span);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in List<T?> value)
    {
        writer.WriteVarUInt32((uint)value.Count);

        var span = CollectionsMarshal.AsSpan(value);
        if (_spanFormatter is null)
        {
            for (var i = 0; i < value.Count; i++)
            {
                _elementFormatter.Serialize(writer, span[i]);
            }
        }
        else
        {
            _spanFormatter.Serialize(writer, span);
        }
    }
}
