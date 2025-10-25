using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Assigns persistent integer IDs to objects based upon their name.
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

    public bool TryGetObject(ulong id, [NotNullWhen(true)] out T? obj)
    {
        return _idToObject.TryGetValue(id, out obj);
    }

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
        Span<byte> nameBytes = stackalloc byte[Encoding.ASCII.GetByteCount(name)];
        Encoding.ASCII.GetBytes(name, nameBytes);
        return XxHash64.HashToUInt64(nameBytes);
    }
}
