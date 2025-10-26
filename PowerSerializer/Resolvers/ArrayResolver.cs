using DouglasDwyer.PowerSerializer.Formatters;
using System;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Create formatters derived from <see cref="ArrayFormatter{T, A}"/>.
/// </summary>
public sealed class ArrayResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        if (type.IsArray)
        {
            return (IFormatter?)Activator.CreateInstance(typeof(ArrayFormatter<,>).MakeGenericType(type.GetElementType()!, type), serializer);
        }
        else
        {
            return null;
        }
    }
}
