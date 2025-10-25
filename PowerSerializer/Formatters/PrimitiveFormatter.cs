namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Handles encoding of primitive types.
/// </summary>
public sealed class PrimitiveFormatter :
    IFormatter<byte>,
    IFormatter<ushort>,
    IFormatter<uint>,
    IFormatter<ulong>,
    IFormatter<sbyte>,
    IFormatter<short>,
    IFormatter<int>,
    IFormatter<long>,
    IFormatter<bool>,
    IFormatter<char>,
    IFormatter<string>,
    IFormatter<object>

{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out byte value) => value = reader.ReadUInt8();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in byte value) => writer.WriteUInt8(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ushort value) => value = reader.ReadUInt16();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ushort value) => writer.WriteUInt16(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out uint value) => value = reader.ReadUInt32();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in uint value) => writer.WriteUInt32(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out ulong value) => value = reader.ReadUInt64();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in ulong value) => writer.WriteUInt64(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out sbyte value) => value = reader.ReadInt8();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in sbyte value) => writer.WriteInt8(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out short value) => value = reader.ReadInt16();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in short value) => writer.WriteInt16(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out int value) => value = reader.ReadInt32();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in int value) => writer.WriteInt32(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out long value) => value = reader.ReadInt64();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in long value) => writer.WriteInt64(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out float value) => value = reader.ReadSingle();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in float value) => writer.WriteSingle(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out double value) => value = reader.ReadDouble();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in double value) => writer.WriteDouble(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out bool value) => value = reader.ReadBool();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in bool value) => writer.WriteBool(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out char value) => value = reader.ReadChar();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in char value) => writer.WriteChar(value);

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out string value) => value = reader.ReadString();

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in string value) => writer.WriteString(value);

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in object value) { }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out object value) => value = new object();
}
