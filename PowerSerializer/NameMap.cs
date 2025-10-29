using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Linq;
using System.Text;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Assigns persistent integer IDs to objects based upon their names.
/// </summary>
/// <typeparam name="T">
/// The type of object that this map will hold.
/// </typeparam>
internal sealed class NameMap<T> where T : class
{
    /// <summary>
    /// Converts from the hash of a name to the original value.
    /// </summary>
    private readonly Dictionary<ulong, T> _idToObject;

    /// <summary>
    /// Converts from a value to its hashed name.
    /// </summary>
    private readonly Dictionary<T, ulong> _objectToId;

    /// <summary>
    /// Constructs a new map for the given collection.
    /// </summary>
    /// <param name="elements">The elements to include in the map.</param>
    /// <param name="nameGetter">
    /// A function that gets a unique name for every element in the iterator.
    /// The name will be hashed using a stable algorithm to produce a persistent ID.
    /// </param>
    public NameMap(IEnumerable<T> elements, Func<T, string> nameGetter)
    {
        var count = elements.Count();
        _idToObject = new Dictionary<ulong, T>(count);
        _objectToId = new Dictionary<T, ulong>(count, ReferenceEqualityComparer.Instance);

        foreach (var element in elements)
        {
            if (!_objectToId.ContainsKey(element))
            {
                var id = HashName(nameGetter(element));
                _idToObject.Add(id, element);
                _objectToId.Add(element, id);
            }
        }
    }

    /// <summary>
    /// Gets the object associated with the given ID, if any.
    /// </summary>
    /// <param name="id">The name hash of the object.</param>
    /// <param name="obj">The object, if found, is written to this variable.</param>
    /// <returns><c>true</c> if an object was found for the ID.</returns>
    public bool TryGetObject(ulong id, [NotNullWhen(true)] out T? obj)
    {
        return _idToObject.TryGetValue(id, out obj);
    }

    /// <summary>
    /// Gets the ID associated with the given object, if any.
    /// </summary>
    /// <param name="obj">The object in question.</param>
    /// <param name="id">The name hash, if found, is written to this variable.</param>
    /// <returns><c>true</c> if the object had an ID.</returns>
    public bool TryGetId(T obj, out ulong id)
    {
        return _objectToId.TryGetValue(obj, out id);
    }

    /// <summary>
    /// Generates a persistent hash from the given name.
    /// </summary>
    /// <param name="name">The name to hash.</param>
    /// <returns>A unique ID.</returns>
    private static ulong HashName(string name)
    {
        Span<byte> nameBytes = stackalloc byte[Encoding.UTF8.GetByteCount(name)];
        Encoding.UTF8.GetBytes(name, nameBytes);
        return XxHash64.HashToUInt64(nameBytes);
    }
}
