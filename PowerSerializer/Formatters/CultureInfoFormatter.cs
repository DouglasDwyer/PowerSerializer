using System.Globalization;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes <see cref="CultureInfo"/> instances.
/// </summary>
public sealed class CultureInfoFormatter : IFormatter<CultureInfo>
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out CultureInfo value)
    {
        value = CultureInfo.GetCultureInfo(reader.ReadInt32());
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in CultureInfo value)
    {
        writer.WriteInt32(value.LCID);
    }
}
