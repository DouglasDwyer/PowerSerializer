using System;
using System.IO;

namespace DouglasDwyer.PowerSerializer.Formatters;

internal class PolymorphicReferenceFormatter<T> : IFormatter<T?> where T : class
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
    public void Deserialize(BufferReader reader, out T? value)
    {
        var reference = ReferenceId.Read(reader);
        if (reference == ReferenceId.Null)
        {
            value = null;
        }
        else if (reference == ReferenceId.New)
        {
            _typeFormatter.Deserialize(reader, out var type);

            if (type is null)
            {
                throw new InvalidDataException("Polymorphic type was not encoded properly: expected type, but got null");
            }

            value = (T)_serializer.GetPolymorphicDispatcher(type).RegisterObjectAndDeserialize(reader);
        }
        else
        {
            value = (T)reader.Context.GetExistingObject(reference.Index);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        var reference = writer.Context.AddOrGetReference(value);
        if (reference == ReferenceId.New)
        {
            var type = value!.GetType();
            _typeFormatter.Serialize(writer, type);
            _serializer.GetPolymorphicDispatcher(type).SerializeValue(writer, value);
        }
    }
}
