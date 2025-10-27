using DouglasDwyer.PowerSerializer.Formatters;
using DouglasDwyer.PowerSerializer.Resolvers;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace DouglasDwyer.PowerSerializer;

public class PowerSerializerOptions
{
    /// <summary>
    /// When the deserializer encounters unknown data, it may need to load types from new assemblies.
    /// This object can be used to control the loading process (and may be useful when deserializing data
    /// with dynamically-loaded assemblies or plugins).
    /// </summary>
    public AssemblyLoadContext AssemblyLoader { get; set; } = AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly()) ?? AssemblyLoadContext.Default;

    /// <summary>
    /// A predefined list of assemblies that are guaranteed to be present during serialization
    /// and deserialization. Type names from these assemblies will be encoded using a short, fixed-size hash.
    /// This can significantly reduce binary size, since otherwise the full type name must be recorded
    /// when serializing a polymorphic object.
    /// </summary>
    public IList<Assembly> KnownAssemblies { get; } = [
        typeof(object).Assembly,
        typeof(IEnumerable).Assembly,
        typeof(IEnumerable<>).Assembly
    ];

    public IList<IFormatterResolver> Resolvers { get; } = [
        new GenericResolver(typeof(AssemblyFormatter)),
        new GenericResolver(typeof(TypeFormatter)),
        new AttributeResolver(),
        new ArrayResolver(),
        new SingletonResolver(new CultureInfoFormatter()),
        new SingletonResolver(new PrimitiveFormatter()),
        new SingletonResolver(new ReferenceEqualityComparerFormatter()),
        new GenericResolver(typeof(KeyValuePairFormatter<,>)),
        new GenericResolver(typeof(ListFormatter<>)),
        new GenericResolver(typeof(QueueFormatter<>)),
        new GenericResolver(typeof(StackFormatter<>)),
        new ComparerCollectionResolver(),
        new CollectionResolver(),
        new GenericResolver(typeof(MemberFormatter<>)),
    ];
}
