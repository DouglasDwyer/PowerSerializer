using Microsoft.Extensions.ObjectPool;
using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer;

internal sealed class SerializationContext : IResettable
{
    public static readonly DefaultObjectPool<SerializationContext> Pool = new DefaultObjectPool<SerializationContext>(new DefaultPooledObjectPolicy<SerializationContext>());

    /// <summary>
    /// A map from object references to associated integer IDs.
    /// </summary>
    private readonly Dictionary<object, uint> _references;

    public SerializationContext()
    {
        _references = new Dictionary<object, uint>(ReferenceEqualityComparer.Instance);
    }

    /// <summary>
    /// Assigns a reference ID to the given object, or returns an existing
    /// ID if the object was already seen.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    /// <returns>An ID associated with the object.</returns>
    public ReferenceId AddOrGetReference(object? obj)
    {
        if (obj is null)
        {
            return ReferenceId.Null;
        }
        else if (_references.TryGetValue(obj, out var id))
        {
            return ReferenceId.Existing(id);
        }
        else
        {
            _references.Add(obj, (uint)_references.Count);
            return ReferenceId.New;
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        _references.Clear();
        return true;
    }
}
