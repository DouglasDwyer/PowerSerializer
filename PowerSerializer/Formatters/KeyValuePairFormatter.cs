using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer.Formatters;

public sealed class KeyValuePairFormatter<K, V> : IFormatter<KeyValuePair<K?, V?>>
{
    private readonly IFormatter<K?> _keyFormatter;
    private readonly IFormatter<V?> _valueFormatter;

    public KeyValuePairFormatter(PowerSerializer serializer)
    {
        _keyFormatter = serializer.GetFormatter<K>();
        _valueFormatter = serializer.GetFormatter<V>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out KeyValuePair<K?, V?> value)
    {
        _keyFormatter.Deserialize(reader, out var pairKey);
        _valueFormatter.Deserialize(reader, out var pairValue);
        value = new KeyValuePair<K?, V?>(pairKey, pairValue);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in KeyValuePair<K?, V?> value)
    {
        _keyFormatter.Serialize(writer, value.Key);
        _valueFormatter.Serialize(writer, value.Value);
    }
}
