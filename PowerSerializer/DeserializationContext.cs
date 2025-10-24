using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.PowerSerializer;

internal class DeserializationContext : IResettable
{
    public static readonly DefaultObjectPool<DeserializationContext> Pool = new DefaultObjectPool<DeserializationContext>(new DefaultPooledObjectPolicy<DeserializationContext>());

    private readonly IRefBox? _newReference;

    /// <summary>
    /// A map from integer IDs to the associated objects.
    /// </summary>
    private readonly List<object> _references;

    public DeserializationContext()
    {
        _references = new List<object>();
    }

    public ref T AddBoxedValueType<T>() where T : struct
    {
        if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is null)
        {
            object result = default(T)!;
            _references.Add(result);
            return ref Unsafe.Unbox<T>(result);
        }
        else
        {
            // todo: same as class
            throw new NotImplementedException();
        }
    }

    public ref T? AddClass<T>() where T : class
    {
        throw new NotImplementedException();
    }

    public T GetExistingObject<T>(uint id) where T : class
    {
        throw new NotImplementedException();
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

    /// <summary>
    /// Provides a mutable memory location for which a <c>ref</c> can be created,
    /// while also allowing access as an <see cref="object"/>.
    /// This allows for strongly-typed reference deserialization.
    /// </summary>
    private sealed class RefBox<T>
    {
        /// <summary>
        /// Gets the object reference.
        /// </summary>
        object? Value { get; }
    }

    /*
    public abstract class RefBox<T>
    {
        public abstract object GetObject();
        public abstract ref T GetRef();
    }

    // Only used for mutable value types
    public sealed class MutableRefBox<T> : RefBox<T> where T : struct
    {
        private readonly object _value;

        public MutableRefBox()
        {
            if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is not null)
            {
                throw new ArgumentException("Creating refs to readonly value types is not allowed", nameof(T));
            }

            _value = new T();
        }

        public override object GetObject() => _value;

        public override ref T GetRef() => ref Unsafe.Unbox<T>(_value);
    }

    // Used for reference types and readonly value types
    public sealed class CopyRefBox<T> : RefBox<T> where T : notnull
    {
        private T? _value;



        public override object GetObject() => _value;

        public override ref T GetRef() => ref _value;
    }*/
}
