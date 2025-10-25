using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.IO;

namespace DouglasDwyer.PowerSerializer;

internal class DeserializationContext : IResettable
{
    public static readonly DefaultObjectPool<DeserializationContext> Pool = new DefaultObjectPool<DeserializationContext>(new DefaultPooledObjectPolicy<DeserializationContext>());

    /// <summary>
    /// A map from integer IDs to the associated objects.
    /// </summary>
    private readonly ObjectRefList _references;

    public DeserializationContext()
    {
        _references = new ObjectRefList();
    }

    /// <summary>
    /// Allocates a new object slot, assigning an index based upon order of occurrence.
    /// An object must be written to this slot before other references are deserialized.
    /// </summary>
    /// <returns>
    /// A stable <c>ref</c> to the slot. This is initially <c>null</c>.
    /// The <c>ref</c> will remain valid for the duration of serialization.
    /// </returns>
    public ref object? AllocateReference()
    {
        CheckLastRefAssigned();
        var index = _references.Count;
        _references.Add(null);
        return ref _references[index];
    }

    /// <summary>
    /// Gets a reference to the previously-added object at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the object.</param>
    /// <returns>A reference to the object itself.</returns>
    /// <exception cref="InvalidDataException">
    /// If the index was out-of-range.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If the object reference was not properly assigned.
    /// </exception>
    public object GetExistingReference(uint index)
    {
        CheckLastRefAssigned();
        if (index < _references.Count)
        {
            return _references[(int)index]!;
        }
        else
        {
            throw new InvalidDataException("Invalid reference ID in deserialization data");
        }
    }

    /// <summary>
    /// Checks that the most recent object reference has been assigned.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If the object still <c>null</c> when it was committed.
    /// </exception>
    private void CheckLastRefAssigned()
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
        _references.Clear();
        return true;
    }


    /// <summary>
    /// Like a <see cref="List{T}"/>, but allows stable <c>ref</c>s to be created for individual elements.
    /// This is accomplished by adding a second level of indirection: the list
    /// stores an array of array "chunks." Elements are written to chunks,
    /// and if space runs out, the <b>outer</b> array is reallocated to hold more chunks.
    /// </summary>
    internal sealed class ObjectRefList
    {
        /// <summary>
        /// The number of elements per underlying allocated array.
        /// </summary>
        private const int ChunkLength = 256;

        /// <summary>
        /// The number of elements contained in the list.
        /// </summary>
        private int _count = 0;

        /// <summary>
        /// The arrays backing this list.
        /// </summary>
        private object?[][] _inner = Array.Empty<object?[]>();

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index to obtain.</param>
        /// <returns>A stable reference to the element at the provided index.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// If the index was beyond the list bounds.
        /// </exception>
        public ref object? this[int index]
        {
            get
            {
                if (index < _count)
                {
                    return ref _inner[index / ChunkLength][index % ChunkLength];
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        /// <inheritdoc/>
        public int Count => _count;

        /// <summary>
        /// Adds an element to the end of the list.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(object? item)
        {
            EnsureCapacity(_count + 1);
            _inner[_count / ChunkLength][_count % ChunkLength] = item;
            _count++;
        }

        /// <summary>
        /// Clears the list contents and discards all object references.
        /// </summary>
        public void Clear()
        {
            var finalChunk = _count / ChunkLength;

            for (var i = 0; i < finalChunk; i++)
            {
                Array.Fill(_inner[i], null);
            }

            _count = 0;
        }

        /// <summary>
        /// Ensures that the capacity of this list is at least the specified <paramref name="capacity"/>.
        /// If the current capacity is less than <paramref name="capacity"/>,
        /// it is increased to at least the specified <paramref name="capacity"/>.
        /// </summary>
        /// <param name="capacity">The new capacity of this list.</param>
        public void EnsureCapacity(int capacity)
        {
            var oldChunkCapacity = _inner.Length;
            var newChunkCapacity = (capacity + ChunkLength - 1) / ChunkLength;
            if (oldChunkCapacity < newChunkCapacity)
            {
                Array.Resize(ref _inner, newChunkCapacity);

                for (var i = oldChunkCapacity; i < newChunkCapacity; i++)
                {
                    _inner[i] = new object?[ChunkLength];
                }
            }
        }
    }
}
