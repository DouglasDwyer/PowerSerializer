using System;
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
    /// Creates a new assembly formatter. The formatter will load assemblies from the current <see cref="AssemblyLoadContext"/>.
    /// </summary>
    public AssemblyFormatter() : this(AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly()) ?? AssemblyLoadContext.Default) { }

    /// <summary>
    /// Creates a new assembly formatter. During deserialization, the formatter will search for missing assemblies from the provided context.
    /// </summary>
    /// <param name="context">
    /// The load context to use.
    /// </param>
    public AssemblyFormatter(AssemblyLoadContext context)
    {
        _assemblyLoader = context;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Assembly value)
    {
        var name = reader.ReadString(Encoding.ASCII);
        var version = new Version(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        value = _assemblyLoader.LoadFromAssemblyName(new AssemblyName() { Name = name, Version = version });
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Assembly value)
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
            writer.WriteString(name, Encoding.ASCII);
            writer.WriteInt32(version?.Major ?? 0);
            writer.WriteInt32(version?.Minor ?? 0);
            writer.WriteInt32(version?.Build ?? 0);
            writer.WriteInt32(version?.Revision ?? 0);
        }
    }
}
