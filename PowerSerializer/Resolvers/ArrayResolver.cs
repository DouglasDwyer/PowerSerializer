using DouglasDwyer.PowerSerializer.Formatters;
using System;

namespace DouglasDwyer.PowerSerializer.Resolvers;

public sealed class ArrayResolver : IFormatterResolver
{
    /// <summary>
    /// The generic formatter type (derived from <see cref="ArrayFormatterBase{T, A}"/>) to use.
    /// </summary>
    private readonly Type _genericType;

    /// <inheritdoc/>
    public IFormatter<T>? GetFormatter<T>(PowerSerializer serializer)
    {
        if (typeof(T).IsArray)
        {
            var type = _genericType.MakeGenericType([typeof(T).GetElementType()!, typeof(T)]);
            return 
        }
        else
        {
            return null;
        }
    }
}
