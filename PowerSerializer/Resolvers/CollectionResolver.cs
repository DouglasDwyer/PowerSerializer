using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Provides formatters for types implementing <see cref="ICollection{T}"/>.
/// </summary>
public sealed class CollectionResolver : IFormatterResolver
{
    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        var collectionInterface = type.GetInterfaces()
            .Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))
            .SingleOrDefault();

        if (collectionInterface is not null)
        {
            var elementType = collectionInterface.GetGenericArguments()[0];
            var formatterType = typeof(CollectionFormatter<,>).MakeGenericType(elementType, type);
            return (IFormatter)Activator.CreateInstance(formatterType, serializer)!;
        }
        else
        {
            return null;
        }
    }
}
