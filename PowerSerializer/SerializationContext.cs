using Microsoft.Extensions.ObjectPool;
using System;
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
    /// Assigns an ID to <paramref name="obj"/> based upon order of occurrence.
    /// </summary>
    /// <param name="obj">The object to add.</param>
    /// <returns>The ID of the new reference.</returns>
    /// <exception cref="ArgumentException">
    /// If <paramref name="obj"/> already had an assigned reference ID.
    /// </exception>
    public uint AllocateReference(object obj)
    {
        var index = (uint)_references.Count;
        _references.Add(obj, (uint)_references.Count);
        return index;
    }

    /// <summary>
    /// Gets the reference ID of <paramref name="obj"/>, if it had one.
    /// </summary>
    /// <param name="obj">The object in question.</param>
    /// <returns>
    /// The associated ID, or <c>null</c> if the object did not have one.
    /// </returns>
    public uint? GetExistingReference(object obj)
    {
        if (_references.TryGetValue(obj, out var id))
        {
            return id;
        }
        else
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        _references.Clear();
        return true;
    }
}
