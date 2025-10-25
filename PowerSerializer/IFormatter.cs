namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Defines how to serialize the contents of an object.
/// This is the core interface that controls the serialization/deserialization process.
/// </summary>
/// <typeparam name="T">
/// The concrete object type to be serialized.
/// </typeparam>
public interface IFormatter<T>
{
    /// <summary>
    /// Writes a binary representation of <paramref name="value"/> to a buffer.
    /// </summary>
    /// <param name="writer">The binary output data.</param>
    /// <param name="value">The object to write.</param>
    void Serialize(BufferWriter writer, in T value);

    /// <summary>
    /// Reads a binary representation of <typeparamref name="T"/> from the buffer.
    /// </summary>
    /// <param name="reader">The binary input data.</param>
    /// <param name="value">
    /// The object that was deserialized.<br/>
    /// 
    /// <b>Important:</b> for reference types, the new object should be assigned
    /// to this variable <b>before</b> invoking <see cref="Deserialize"/> for any children.
    /// In order to support cyclic references, the deserializer assigns a special slot
    /// to this <c>out</c> parameter. The deserializer will read the slot when a recursive
    /// reference is requested, but for that to work, the object must already be set.
    /// </param>
    void Deserialize(BufferReader reader, out T value);
}
