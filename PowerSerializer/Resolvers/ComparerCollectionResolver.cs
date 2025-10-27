using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Generates formatters for collection types that have an <see cref="IComparer{T}"/> <see cref="IEqualityComparer{T}"/>.
/// </summary>
public sealed class ComparerCollectionResolver : IFormatterResolver
{
    /// <summary>
    /// Collection types supported by this resolver.
    /// </summary>
    private readonly ImmutableHashSet<Type> Collections = [
        typeof(Dictionary<,>),
        typeof(HashSet<>),
        typeof(OrderedDictionary<,>),
        typeof(PriorityQueue<,>),
        typeof(SortedDictionary<,>),
        typeof(SortedList<,>),
        typeof(SortedSet<>)
    ];

    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        if (type.IsConstructedGenericType
            && Collections.Contains(type.GetGenericTypeDefinition()))
        {
            var collectionInterface = type.GetInterfaces().Single(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
            var elementType = collectionInterface.GetGenericArguments().Single();
            var keyType = type.GetProperty("Comparer")!.PropertyType.GetGenericArguments().Single();

            var formatterType = typeof(ComparerCollectionFormatter<,,>).MakeGenericType(keyType, elementType, type);
            return (IFormatter)Activator.CreateInstance(formatterType, [serializer])!;
        }
        else
        {
            return null;
        }
    }
}
