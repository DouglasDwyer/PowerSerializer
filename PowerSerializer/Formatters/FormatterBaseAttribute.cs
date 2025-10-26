using DouglasDwyer.PowerSerializer.Resolvers;
using System;
using System.Linq;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Used in conjunction with <see cref="AttributeResolver"/>.
/// This attribute (or derived variants thereof) can be added to types
/// to specify how they are serialized.
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public abstract class FormatterBaseAttribute : Attribute
{
    /// <summary>
    /// The formatter to use on the type where this attribute is applied.
    /// </summary>
    public readonly Type Formatter;

    /// <summary>
    /// Creates a new formatter attribute.
    /// </summary>
    /// <param name="formatter">
    /// The formatter to use on the type where this attribute is applied.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="formatter"/> does not implement <see cref="IFormatter{T}"/>.
    /// </exception>
    public FormatterBaseAttribute(Type formatter)
    {
        if (!formatter.GetInterfaces().Any(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IFormatter<>)))
        {
            throw new ArgumentException("Type did not implement the IFormatter<T> interface for at least one type", nameof(formatter));
        }

        if (formatter.GetConstructor([]) is null && formatter.GetConstructor([typeof(PowerSerializer)]) is null)
        {
            throw new ArgumentException("Type did not have a public parameterless constructor or public constructor taking a PowerSerializer instance", nameof(formatter));
        }

        Formatter = formatter;
    }
}