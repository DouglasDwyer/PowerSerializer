using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;

namespace DouglasDwyer.PowerSerializer;

public class PowerSerializerOptions
{
    public AssemblyLoadContext AssemblyLoader { get; set; } = AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly()) ?? AssemblyLoadContext.Default;

    public IList<Assembly> KnownAssemblies { get; } = [
        typeof(object).Assembly,
        typeof(IEnumerable).Assembly,
        typeof(IEnumerable<>).Assembly
    ];
}
