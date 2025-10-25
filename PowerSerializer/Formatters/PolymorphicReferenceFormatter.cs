using System;
using System.IO;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes objects of type <typeparamref name="T"/> with polymorphism.
/// The type of the object is serialized along with its contents,
/// meaning that the deserializer can determine its true type.
/// </summary>
/// <typeparam name="T">
/// The concrete type to serialize.
/// </typeparam>
internal sealed class PolymorphicReferenceFormatter<T> : ReferenceFormatterBase<T> where T : class
{
    /// <summary>
    /// The serializer from which to dynamically fetch formatters.
    /// </summary>
    private readonly PowerSerializer _serializer;

    /// <summary>
    /// The formatter to use for recording object types.
    /// </summary>
    private readonly IFormatter<Type?> _typeFormatter;

    /// <summary>
    /// Creates a new polymorphic formatter.
    /// </summary>
    /// <param name="serializer">
    /// The serializer from which to dynamically fetch formatters.
    /// </param>
    public PolymorphicReferenceFormatter(PowerSerializer serializer)
    {
        _serializer = serializer;
        _typeFormatter = _serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    protected override T RegisterObjectAndDeserialize(BufferReader reader)
    {
        _typeFormatter.Deserialize(reader, out var type);

        if (type is null)
        {
            throw new InvalidDataException("Polymorphic type was not encoded properly: expected type, but got null");
        }

        return (T)_serializer.GetPolymorphicDispatcher(type).RegisterObjectAndDeserialize(reader);
    }

    /// <inheritdoc/>
    protected override void SerializeValue(BufferWriter writer, T value)
    {
        _serializer.GetPolymorphicDispatcher(value.GetType()).SerializeValue(writer, value);
    }
}
