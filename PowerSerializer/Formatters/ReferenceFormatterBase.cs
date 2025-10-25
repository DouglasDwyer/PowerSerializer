using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Implements the logic for serializing and deserializing reference types - namely,
/// the conversion from objects to IDs.
/// </summary>
/// <typeparam name="T"></typeparam>
internal abstract class ReferenceFormatterBase<T> : IFormatter<T?> where T : class
{
    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T? value)
    {
        var reference = ReferenceId.Read(reader);
        if (reference == ReferenceId.Null)
        {
            value = null;
        }
        else if (reference == ReferenceId.New)
        {
            value = RegisterObjectAndDeserialize(reader);
        }
        else
        {
            value = (T)reader.Context.GetExistingReference(reference.Index);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        if (value is null)
        {
            ReferenceId.Null.Write(writer);
        }
        else if (writer.Context.GetExistingReference(value) is uint id)
        {
            ReferenceId.Existing(id).Write(writer);
        }
        else
        {
            writer.Context.AllocateReference(value);
            ReferenceId.New.Write(writer);
            SerializeValue(writer, value);
        }
    }

    /// <summary>
    /// Deserializes the contents of <paramref name="reader"/> into a new object,
    /// recording the object reference in the reader's context.
    /// </summary>
    /// <param name="reader">
    /// The input buffer.
    /// </param>
    /// <returns>
    /// The object that was deserialized.
    /// </returns>
    protected abstract T RegisterObjectAndDeserialize(BufferReader reader);

    /// <summary>
    /// Serializes the contents of <paramref name="value"/> to the output buffer.
    /// No casting or reference recording is performed.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="value">The value to record.</param>
    protected abstract void SerializeValue(BufferWriter writer, T value);

    /// <summary>
    /// Encodes an object reference.
    /// </summary>
    private readonly record struct ReferenceId
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
}
