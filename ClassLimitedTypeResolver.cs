using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="FinalizerLimitedTypeResolver"/> allow for serialization of all the classes specified in the constructor, as well as all structs loaded at resolver construction. They utilize the MD5 hash of a type's name in order to resolve it across serialization boundaries.
    /// </summary>
    public class ClassLimitedTypeResolver : GuidTypeResolver
    {
        /// <summary>
        /// Creates a new resolver instance, allowing all currently-loaded classes in the current assembly to be serialized.
        /// </summary>
        public ClassLimitedTypeResolver() : this(Assembly.GetCallingAssembly().GetTypes()) { }
        /// <summary>
        /// Creates a new resolver instance, allowing all currently-loaded classes in the given assemblies to be serialized.
        /// </summary>
        public ClassLimitedTypeResolver(params Assembly[] assemblies) : this((IEnumerable<Assembly>)assemblies) { }
        /// <summary>
        /// Creates a new resolver instance, allowing all currently-loaded classes in the given assemblies to be serialized.
        /// </summary>
        public ClassLimitedTypeResolver(IEnumerable<Assembly> assemblies) : this(assemblies.SelectMany(x => x.GetTypes())) { }
        /// <summary>
        /// Creates a new resolver instance, allowing all of the given classes to be serialized.
        /// </summary>
        public ClassLimitedTypeResolver(params Type[] types) : this((IEnumerable<Type>)types) { }
        /// <summary>
        /// Creates a new resolver instance, allowing all of the given classes to be serialized.
        /// </summary>
        public ClassLimitedTypeResolver(IEnumerable<Type> types) : base(IncludeAllPrimitiveAndStructTypes(types)) { }

        private static IEnumerable<Type> IncludeAllPrimitiveAndStructTypes(IEnumerable<Type> types)
        {
            return types.Concat(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsValueType || x == typeof(string))).Distinct();
        }
    }
}
