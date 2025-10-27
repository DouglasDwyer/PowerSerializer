using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes <see cref="ReferenceEqualityComparer"/> objects.
/// </summary>
public sealed class ReferenceEqualityComparerFormatter : IFormatter<ReferenceEqualityComparer>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ReferenceEqualityComparer value)
    {
        value = ReferenceEqualityComparer.Instance;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ReferenceEqualityComparer value) { }
}
