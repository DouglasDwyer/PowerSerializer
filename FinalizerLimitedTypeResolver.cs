using System;
using System.Reflection;
using System.Linq;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="FinalizerLimitedTypeResolver"/> allow for serialization of any type loaded in the current <see cref="AppDomain"/> upon resolver construction, provided that the type doesn't have a finalizer. They utilize the MD5 hash of a type's name in order to resolve it across serialization boundaries.
    /// </summary>
    public class FinalizerLimitedTypeResolver : GuidTypeResolver
    {
        /// <summary>
        /// Creates a new resolver instance, allowing all currently-loaded types without a finalizer to be serialized.
        /// </summary>
        public FinalizerLimitedTypeResolver() : base(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => !HasFinalizer(x)))
        {
        }

        private static bool HasFinalizer(Type type)
        {
            MethodInfo method = type.GetMethod("Finalize",
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);
            if (method is null || type == typeof(object))
            {
                return false;
            }
            else
            {
                return method.GetBaseDefinition() == typeof(object).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
    }
}
