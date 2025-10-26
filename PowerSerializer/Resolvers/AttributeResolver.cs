using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Searches type definitions for a specific attribute, which specifies a custom formatter.
/// If the attribute is found on a type, then the custom formatter is used to serialize it.
/// </summary>
public sealed class AttributeResolver : IFormatterResolver
{
    /// <summary>
    /// If a type specifies this attribute, then the formatter from the attribute will be used to serialize it.
    /// </summary>
    public Type AttributeType { get; }

    /// <summary>
    /// Creates a resolver that will search for the <see cref="DefaultFormatterAttribute"/>.
    /// </summary>
    public AttributeResolver() : this(typeof(DefaultFormatterAttribute)) { }

    /// <summary>
    /// Creates a resolver that will search for the specified attribute.
    /// </summary>
    /// <param name="attributeType">
    /// If a type specifies this attribute, then the formatter from the attribute will be used to serialize it.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="attributeType"/> did not inherit from <see cref="FormatterBaseAttribute"/>.
    /// </exception>
    public AttributeResolver(Type attributeType)
    {
        if (!attributeType.IsAssignableTo(typeof(FormatterBaseAttribute)))
        {
            throw new ArgumentException("The formatter attribute must inherit from FormatterBaseAttribute", nameof(attributeType));
        }

        AttributeType = attributeType;
    }

    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        var attribute = (FormatterBaseAttribute?)type.GetCustomAttribute(AttributeType);

        if (attribute is null)
        {
            return null;
        }
        else
        {
            if (attribute.Formatter.GetConstructor([typeof(PowerSerializer)]) is not null)
            {
                return (IFormatter)Activator.CreateInstance(attribute.Formatter, serializer)!;
            }
            else
            {
                return (IFormatter)Activator.CreateInstance(attribute.Formatter)!;
            }
        }
    }
}
