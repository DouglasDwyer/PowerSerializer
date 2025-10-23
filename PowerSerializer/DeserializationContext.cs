using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;

namespace DouglasDwyer.PowerSerializer;

internal class DeserializationContext : IResettable
{
    public static readonly DefaultObjectPool<DeserializationContext> Pool = new DefaultObjectPool<DeserializationContext>(new DefaultPooledObjectPolicy<DeserializationContext>());

    /// <summary>
    /// A map from integer IDs to the associated objects.
    /// </summary>
    private readonly List<object> _references;

    public DeserializationContext()
    {
        _references = new List<object>();
    }

    public ReferenceHolder<T>? GetOrCreateReference<T>(Reference reference)
    {
        if (reference == Reference.Null)
        {
            return null;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    /// <inheritdoc/>
    public bool TryReset()
    {
        _references.Clear();
        return true;
    }

    public interface IReferenceHolder<out T>
    {
        T? GetValue();
    }

    public sealed class ReferenceHolder<T> : IReferenceHolder<T>
    {
        public T? Value;

        public T? GetValue() => Value;
    }
}
