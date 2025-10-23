using System;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Encodes an object reference.
/// </summary>
internal readonly record struct Reference
{
    /// <summary>
    /// The reference corresponds to a new object.
    /// </summary>
    public static readonly Reference New = new Reference(1);

    /// <summary>
    /// The reference is null, and does not correspond to any object.
    /// </summary>
    public static readonly Reference Null = new Reference(0);

    private readonly uint _inner;

    private Reference(uint inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// The reference corresponds to an existing object that has been seen before.
    /// </summary>
    /// <param name="id">The ID of the existing object.</param>
    /// <returns>An encoded reference for that ID.</returns>
    public static Reference Existing(uint id)
    {
        if (1 < id)
        {
            return new Reference(id);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Invalid reference ID");
        }
    }

    // todo: make this variable-length encoding since we know it always starts from 0
    public static Reference Read(BufferReader reader)
    {
        return new Reference(reader.ReadUInt32());
    }

    public void Write(BufferWriter writer)
    {
        writer.WriteUInt32(_inner);
    }
}