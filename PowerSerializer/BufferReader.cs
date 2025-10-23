using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Allows for decoding binary data and common primitives during deserialization.
/// </summary>
public ref struct BufferReader
{
    /// <summary>
    /// Shared deserializer state.
    /// </summary>
    internal readonly DeserializationContext Context;

    /// <summary>
    /// The entire buffer being read.
    /// </summary>
    private readonly ReadOnlySpan<byte> _data;

    /// <summary>
    /// The index of the next byte to read from <see cref="_data"/>.
    /// </summary>
    private ref int _position;

    /// <summary>
    /// Creates a new buffer reader.
    /// </summary>
    /// <param name="context">Shared deserializer state.</param>
    /// <param name="data">The data to read.</param>
    /// <param name="position">A variable to track the position.</param>
    internal BufferReader(DeserializationContext context, ReadOnlySpan<byte> data, ref int position)
    {
        Context = context;
        _data = data;
        _position = ref position;
    }

    /// <summary>
    /// Reads <paramref name="count"/> bytes from the buffer, then advances the cursor.
    /// </summary>
    /// <param name="count">The amount of data to read.</param>
    /// <returns>A span containing the bytes that were read.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is negative or exceeds the buffer bounds.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Read(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count may not be negative negative");
        }

        var nextPosition = _position + count;

        if (nextPosition < count || _data.Length < nextPosition)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Exceeded the end of underlying buffer");
        }

        var result = _data[_position..nextPosition];
        _position = nextPosition;
        return result;
    }

    /// <summary>
    /// Reads a value from the buffer.
    /// </summary>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadUInt8()
    {
        return Read(sizeof(byte))[0];
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(Read(sizeof(ushort)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(Read(sizeof(uint)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(Read(sizeof(ulong)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadInt8()
    {
        return (sbyte)Read(sizeof(sbyte))[0];
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(Read(sizeof(short)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(Read(sizeof(int)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(Read(sizeof(long)));
    }

    /// <summary>
    /// Reads a value from the buffer using variable-length encoding.
    /// </summary>
    /// <remarks>
    /// This function compresses the leading zeroes in the integer's binary representation.
    /// Therefore, it works best for small numbers.
    /// </remarks>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadVarUInt16()
    {
        return (ushort)ReadVarUInt(sizeof(ushort));
    }

    /// <inheritdoc cref="ReadVarUInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadVarUInt32()
    {
        return (uint)ReadVarUInt(sizeof(uint));
    }

    /// <inheritdoc cref="ReadVarUInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadVarUInt64()
    {
        return ReadVarUInt(sizeof(ulong));
    }

    /// <summary>
    /// Reads a value from the buffer using variable-length encoding.
    /// </summary>
    /// <remarks>
    /// This function uses <see href="https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding">zigzag encoding</see>
    /// to support positive and negative numbers. It works best with numbers that have small absolute value.
    /// </remarks>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadVarInt16()
    {
        return (short)ReadVarInt(sizeof(short));
    }

    /// <inheritdoc cref="ReadVarInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadVarInt32()
    {
        return (int)ReadVarInt(sizeof(int));
    }

    /// <inheritdoc cref="ReadVarInt16"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadVarInt64()
    {
        return ReadVarInt(sizeof(long));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle()
    {
        return BinaryPrimitives.ReadSingleLittleEndian(Read(sizeof(float)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadDouble()
    {
        return BinaryPrimitives.ReadDoubleLittleEndian(Read(sizeof(double)));
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ReadBool()
    {
        switch (ReadUInt8())
        {
            case 0:
                return false;
            case 1:
                return true;
            default:
                throw new InvalidDataException("Expected boolean value to be 0 or 1");
        }
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public char ReadChar()
    {
        return (char)ReadUInt16();
    }

    /// <inheritdoc cref="ReadUInt8"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString()
    {
        return ReadString(Encoding.Unicode);
    }

    /// <summary>
    /// Reads a string from the buffer with the given encoding.
    /// </summary>
    /// <param name="encoding">The encoding to use.</param>
    /// <returns>The value that was read.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadString(Encoding encoding)
    {
        var bytes = (int)ReadVarUInt32();
        return encoding.GetString(Read(bytes));
    }

    /// <summary>
    /// Reads a variable-length integer from the input data.
    /// </summary>
    /// <remarks>
    /// This function uses <see href="https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding">zigzag encoding</see>
    /// to support positive and negative numbers. It works best with numbers that have small absolute value.
    /// </remarks>
    /// <param name="bytes">The maximum number of allowed bytes.</param>
    /// <returns>The integer that was decoded.</returns>
    /// <exception cref="InvalidDataException">If the input data was not formatted correctly.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ReadVarInt(int bytes)
    {
        var zigzag = ReadVarUInt(bytes);

        if ((zigzag & 1) != 0)
        {
            return (-1 * ((long)(zigzag >> 1) + 1));
        }

        return (long)(zigzag >> 1);
    }

    /// <summary>
    /// Reads a variable-length integer from the input data.
    /// </summary>
    /// <remarks>
    /// This function compresses the leading zeroes in the integer's binary representation.
    /// Therefore, it works best for nonnegative numbers.
    /// </remarks>
    /// <param name="bytes">The maximum number of allowed bytes.</param>
    /// <returns>The integer that was decoded.</returns>
    /// <exception cref="InvalidDataException">If the input data was not formatted correctly.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong ReadVarUInt(int bytes)
    {
        var bits = 8 * bytes;
        var result = 0ul;
        var shift = 0;
        
        while (true)
        {
            var next = ReadUInt8();
            result |= (ulong)(next & 0b0111_1111) << shift;

            if ((next & 0b1000_0000) == 0)
            {
                break;
            }

            shift += 7;

            if (bits < shift)
            {
                throw new InvalidDataException("Malformed variable-length integer");
            }
        }

        return result;
    }
}