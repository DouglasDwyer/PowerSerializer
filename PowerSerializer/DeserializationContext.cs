using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer;

internal class DeserializationContext : IResettable
{
    public static readonly DefaultObjectPool<DeserializationContext> Pool = new DefaultObjectPool<DeserializationContext>(new DefaultPooledObjectPolicy<DeserializationContext>());

    /// <summary>
    /// A map from integer IDs to the associated objects.
    /// </summary>
    private readonly List<object?> _references;

    public DeserializationContext()
    {
        _references = new List<object?>();
    }

    public ref object? AddObject()
    {
        CheckReferencesAssigned();
        var index = _references.Count;
        _references.Add(null);
        return ref CollectionsMarshal.AsSpan(_references)[index];
    }

    /// <summary>
    /// Gets a reference to the previously-added object at <paramref name="index"/>.
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">
    /// If the index was out-of-range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If the object reference was not properly assigned.
    /// </exception>
    public object GetExistingObject(uint index)
    {
        CheckReferencesAssigned();
        if (index < _references.Count)
        {
            return _references[(int)index];
        }
        else
        {
            throw new InvalidDataException("Invalid reference ID in deserialization data");
        }
    }

    /// <summary>
    /// Checks that all existing object references have been assigned.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If the object still <c>null</c> when it was committed.
    /// </exception>
    private void CheckReferencesAssigned()
    {
        if (0 < _references.Count)
        {
            if (_references[_references.Count - 1] is null)
            {
                throw new InvalidOperationException("Attempted to deserialize a child reference before initializing the parent. "
                    + "The `out T? value` argument of IFormatter<T>.Deserialize must be written before deserializing other references. "
                    + "Otherwise, cyclic reference resolution would not work.");
            }
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        // Get RefBox for new object.
        //   This allocates the box, beginning a "fill box" operation
        // When RefBox is next called, check to see if box has been filled.
        //   If so, commit object to _references list.
        //   Make new refbox
        // If GetObject is called and there is an active unfilled RefBox, throw exception
        // After call completes, commit RefBox... do I even need to..?

        _references.Clear();
        return true;
    }
}
