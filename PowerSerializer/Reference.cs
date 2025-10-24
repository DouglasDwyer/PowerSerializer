using System;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Encodes an object reference.
/// </summary>
internal readonly record struct ReferenceId
{
    /// <summary>
    /// The reference corresponds to a new object.
    /// </summary>
    public static readonly ReferenceId New = new ReferenceId(1);

    /// <summary>
    /// The reference is null, and does not correspond to any object.
    /// </summary>
    public static readonly ReferenceId Null = new ReferenceId(0);
    
    /// <summary>
    /// Gets the index associated with this ID.
    /// </summary>
    public uint Index
    {
        get
        {
            if (2 <= _inner)
            {
                return _inner - 2;
            }
            else
            {
                throw new InvalidOperationException("Reference did not correspond to an index");
            }
        }
    }

    /// <summary>
    /// The inner representation of the reference.
    /// </summary>
    private readonly uint _inner;

    /// <summary>
    /// Creates a new reference ID.
    /// </summary>
    /// <param name="inner">The inner representation of the ID.</param>
    private ReferenceId(uint inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// The reference corresponds to an existing object that has been seen before.
    /// </summary>
    /// <param name="id">The ID of the existing object.</param>
    /// <returns>An encoded reference for that ID.</returns>
    public static ReferenceId Existing(uint id)
    {
        return new ReferenceId(id + 2);
    }

    /// <summary>
    /// Decodes the reference ID from the input.
    /// </summary>
    /// <param name="reader">The input buffer.</param>
    /// <returns>The decoded reference ID.</returns>
    public static ReferenceId Read(BufferReader reader)
    {
        return new ReferenceId(reader.ReadVarUInt32());
    }

    /// <summary>
    /// Encodes the reference ID and writes it to the output.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    public void Write(BufferWriter writer)
    {
        writer.WriteVarUInt32(_inner);
    }
}