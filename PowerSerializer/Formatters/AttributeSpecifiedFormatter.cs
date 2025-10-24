using System;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Formatters;

public sealed class AttributeSpecifiedFormatter<T> : IFormatter<T>
{
    /// <summary>
    /// The formatter that was specified by an attribute on <typeparamref name="T"/>.
    /// </summary>
    private readonly IFormatter<T> _inner;
    
    public AttributeSpecifiedFormatter(PowerSerializer serializer, Type formatterAttributeType)
    {
        if (!formatterAttributeType.IsAssignableTo(typeof(FormatterBaseAttribute)))
        {
            throw new ArgumentException("The formatter attribute must inherit from FormatterBaseAttribute", nameof(formatterAttributeType));
        }

        var attribute = (FormatterBaseAttribute?)typeof(T).GetCustomAttribute(formatterAttributeType);

        if (attribute is null)
        {
            throw new ArgumentException("Type did not have required formatter attribute", nameof(T));
        }
        else
        {
            if (attribute.Formatter.GetConstructor([typeof(PowerSerializer)]) is not null)
            {
                _inner = (IFormatter<T>)Activator.CreateInstance(attribute.Formatter, serializer)!;
            }
            else
            {
                _inner = (IFormatter<T>)Activator.CreateInstance(attribute.Formatter)!;
            }
        }
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        _inner.Deserialize(reader, out value);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        _inner.Serialize(writer, value);
    }
}
