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

    public Reference RecordReference(object? obj)
    {
        if (obj is null)
        {
            return Reference.Null;
        }
        else if (_references.TryGetValue(obj, out var id))
        {
            return Reference.Existing(id);
        }
        else
        {
            _references.Add(obj, (uint)_references.Count);
            return Reference.New;
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        _references.Clear();
        return true;
    }
}
