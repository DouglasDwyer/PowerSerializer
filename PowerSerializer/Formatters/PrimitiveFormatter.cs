using System.Runtime.CompilerServices;

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
    IFormatter<float>,
    IFormatter<double>,
    IFormatter<decimal>,
    IFormatter<bool>,
    IFormatter<char>,
    IFormatter<string>,
    IFormatter<object>

{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out byte value) => value = reader.ReadUInt8();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in byte value) => writer.WriteUInt8(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out ushort value) => value = reader.ReadUInt16();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in ushort value) => writer.WriteUInt16(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out uint value) => value = reader.ReadUInt32();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in uint value) => writer.WriteUInt32(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out ulong value) => value = reader.ReadUInt64();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in ulong value) => writer.WriteUInt64(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out sbyte value) => value = reader.ReadInt8();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in sbyte value) => writer.WriteInt8(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out short value) => value = reader.ReadInt16();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in short value) => writer.WriteInt16(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out int value) => value = reader.ReadInt32();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in int value) => writer.WriteInt32(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out long value) => value = reader.ReadInt64();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in long value) => writer.WriteInt64(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out float value) => value = reader.ReadSingle();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in float value) => writer.WriteSingle(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out double value) => value = reader.ReadDouble();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in double value) => writer.WriteDouble(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out decimal value) => value = reader.ReadDecimal();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in decimal value) => writer.WriteDecimal(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out bool value) => value = reader.ReadBool();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in bool value) => writer.WriteBool(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out char value) => value = reader.ReadChar();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in char value) => writer.WriteChar(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out string value) => value = reader.ReadString();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in string value) => writer.WriteString(value);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Serialize(BufferWriter writer, in object value) { }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deserialize(BufferReader reader, out object value) => value = new object();
}
