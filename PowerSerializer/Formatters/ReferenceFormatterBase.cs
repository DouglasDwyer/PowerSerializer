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
}
