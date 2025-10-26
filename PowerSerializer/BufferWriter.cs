using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Allows for encoding binary data and common primitives during serialization.
/// </summary>
public ref struct BufferWriter
{
    /// <summary>
    /// Shared serializer state.
    /// </summary>
    internal readonly SerializationContext Context;

    private readonly IBufferWriter<byte> _writer;

    internal BufferWriter(SerializationContext context, IBufferWriter<byte> writer)
    {
        Context = context;
        _writer = writer;
    }

    /// <summary>
    /// Records <paramref name="count"/> bytes as being written, advancing the cursor.
    /// This may be used in conjunction with <see cref="GetSpan"/> to obtain a buffer,
    /// write some bytes, and then report the final length afterward.
    /// </summary>
    /// <param name="count">The number of bytes that were written.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        _writer.Advance(count);
    }

    /// <summary>
    /// Gets a segment of the output buffer without advancing the cursor.
    /// The segment should be modified, and then <see cref="Advance"/>
    /// should be called to report the number of bytes written.<br/>
    /// 
    /// Note that calling other methods (such as <see cref="Write"/>)
    /// may cause a reallocation, invalidating the returned span.
    /// The span should only be modified immediately after this call.
    /// </summary>
    /// <param name="count">The desired size of the span, in bytes.</param>
    /// <returns>A reference to the output buffer's memory.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> GetSpan(int count)
    {
        return _writer.GetSpan(count)[..count];
    }

    /// <summary>
    /// Copies <paramref name="data"/> to the output buffer and advances the cursor.
    /// </summary>
    /// <param name="data">The data to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(scoped ReadOnlySpan<byte> data)
    {
        data.CopyTo(GetSpan(data.Length));
        Advance(data.Length);
    }

    /// <summary>
    /// Writes a value to the buffer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt8(byte value)
    {
        Write(new ReadOnlySpan<byte>(ref value));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(GetSpan(sizeof(ushort)), value);
        Advance(sizeof(ushort));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(GetSpan(sizeof(uint)), value);
        Advance(sizeof(uint));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(GetSpan(sizeof(ulong)), value);
        Advance(sizeof(ulong));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt8(sbyte value)
    {
        WriteUInt8((byte)value);
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16(short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(GetSpan(sizeof(short)), value);
        Advance(sizeof(short));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(GetSpan(sizeof(int)), value);
        Advance(sizeof(int));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(GetSpan(sizeof(long)), value);
        Advance(sizeof(long));
    }

    /// <summary>
    /// Writes a value to the buffer using variable-length encoding.
    /// </summary>
    /// <remarks>
    /// This function compresses the leading zeroes in the integer's binary representation.
    /// Therefore, it works best for nonnegative numbers.
    /// </remarks>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt16(ushort value)
    {
        WriteVarUInt(value);
    }

    /// <inheritdoc cref="WriteVarUInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt32(uint value)
    {
        WriteVarUInt(value);
    }

    /// <inheritdoc cref="WriteVarUInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarUInt64(ulong value)
    {
        WriteVarUInt(value);
    }

    /// <summary>
    /// Writes a value to the buffer using variable-length encoding.
    /// </summary>
    /// <remarks>
    /// This function uses <see href="https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding">zigzag encoding</see>
    /// to support positive and negative numbers. It works best with numbers that have small absolute value.
    /// </remarks>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt16(short value)
    {
        WriteVarInt(value);
    }

    /// <inheritdoc cref="WriteVarInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt32(int value)
    {
        WriteVarInt(value);
    }

    /// <inheritdoc cref="WriteVarInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteVarInt64(long value)
    {
        WriteVarInt(value);
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteSingle(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(GetSpan(sizeof(float)), value);
        Advance(sizeof(float));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDouble(double value)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(GetSpan(sizeof(double)), value);
        Advance(sizeof(double));
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteDecimal(decimal value)
    {
        Span<int> output = stackalloc int[sizeof(decimal) / sizeof(int)];
        decimal.GetBits(value, output);
        
        foreach (var section in output)
        {
            WriteInt32(section);
        }
    }

    /// <inheritdoc cref="WriteUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBool(bool value)
    {
        WriteUInt8(value ? (byte)1 : (byte)0);
    }

    /// <inheritdoc cref="WriteUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteChar(char value)
    {
        WriteUInt16(value);
    }

    /// <inheritdoc cref="WriteUInt8(byte)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value)
    {
        WriteString(value, Encoding.Unicode);
    }

    /// <summary>
    /// Writes a value to the buffer with the given encoding.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteString(string value, Encoding encoding)
    {
        var bytes = encoding.GetByteCount(value);
        WriteVarUInt32((uint)bytes);
        var buffer = GetSpan(bytes);
        encoding.GetBytes(value, buffer);
        Advance(bytes);
    }

    /// <summary>
    /// Writes a variable-length integer to the buffer.
    /// </summary>
    /// <remarks>
    /// This function uses <see href="https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding">zigzag encoding</see>
    /// to support positive and negative numbers. It works best with numbers that have small absolute value.
    /// </remarks>
    /// <param name="value">The value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteVarInt(long value)
    {
        var zigzag = (ulong)((value << 1) ^ (value >> (64 - 1)));
        WriteVarUInt(zigzag);
    }

    /// <summary>
    /// Writes a variable-length integer to the buffer.
    /// </summary>
    /// <remarks>
    /// This function compresses the leading zeroes in the integer's binary representation.
    /// Therefore, it works best for nonnegative numbers.
    /// </remarks>
    /// <param name="value">The value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteVarUInt(ulong value)
    {
        var remaining = value;

        while (true)
        {
            var toWrite = (byte)(remaining & 0b0111_1111);
            remaining = remaining >> 7;

            if (0 < remaining)
            {
                WriteUInt8((byte)(toWrite | 0b1000_0000));
            }
            else
            {
                WriteUInt8(toWrite);
                break;
            }
        }
    }
}