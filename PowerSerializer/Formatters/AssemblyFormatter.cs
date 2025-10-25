using System;
using System.IO.Hashing;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes <see cref="Assembly"/> objects by their name and version.
/// </summary>
public sealed class AssemblyFormatter : IFormatter<Assembly>
{
    /// <summary>
    /// Where to find new assemblies.
    /// </summary>
    private readonly AssemblyLoadContext _assemblyLoader;

    /// <summary>
    /// A mapping for shortening well-known assembly names to 8-byte IDs.
    /// </summary>
    private readonly NameMap<Assembly> _knownAssemblies;

    /// <summary>
    /// Creates a new assembly formatter. During deserialization, the formatter will search for missing assemblies from the provided context.
    /// </summary>
    /// <param name="serializer">
    /// The serializer associated with this formatter.
    /// </param>
    public AssemblyFormatter(PowerSerializer serializer)
    {
        _assemblyLoader = serializer.Options.AssemblyLoader;
        _knownAssemblies = new NameMap<Assembly>(serializer.Options.KnownAssemblies.Where(x => !x.IsDynamic), x => x.GetName().Name!);
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Assembly value)
    {
        var wellKnown = reader.ReadBool();
        if (wellKnown)
        {
            var id = reader.ReadUInt64();
            if (!_knownAssemblies.TryGetObject(id, out value!))
            {
                throw new TypeLoadException("Could not find well-known assembly by hash; an assembly may be missing from the PowerSerializerOptions.KnownAssemblies list");
            }
        }
        else
        {
            var name = reader.ReadString(Encoding.ASCII);
            var version = new Version(reader.ReadVarInt32(), reader.ReadVarInt32(), reader.ReadVarInt32(), reader.ReadVarInt32());
            value = _assemblyLoader.LoadFromAssemblyName(new AssemblyName() { Name = name, Version = version });
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Assembly value)
    {
        if (_knownAssemblies.TryGetId(value, out var id))
        {
            writer.WriteBool(true);
            writer.WriteUInt64(id);
        }
        else
        {
            var qualifiedName = value.GetName();
            var name = qualifiedName.Name;
            var version = qualifiedName.Version;

            if (name is null)
            {
                throw new NotSupportedException("Serializing runtime-generated assemblies not supported");
            }
            else
            {
                writer.WriteBool(false);
                writer.WriteString(name, Encoding.ASCII);
                writer.WriteVarInt32(version?.Major ?? 0);
                writer.WriteVarInt32(version?.Minor ?? 0);
                writer.WriteVarInt32(version?.Build ?? 0);
                writer.WriteVarInt32(version?.Revision ?? 0);
            }
        }
    }
}
