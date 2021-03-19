using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="FinalizerLimitedNameTypeResolver"/> allow for serialization of any type loaded in the current <see cref="AppDomain"/> upon resolver construction, provided that the type doesn't have a finalizer. They utilize the a type's assembly-qualified name in order to resolve it across serialization boundaries.
    /// </summary>
    public class FinalizerLimitedNameTypeResolver : NameTypeResolver
    {
        /// <summary>
        /// Returns whether the given type should be serialized or deserialized.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>Whether the type is serializable.</returns>
        public override bool IsTypeSerializable(Type type)
        {
            return type.IsValueType || !HasFinalizer(type);
        }

        private static bool HasFinalizer(Type type)
        {
            if (type == typeof(object) || type is null)
            {
                return false;
            }
            MethodInfo method = type.GetMethod("Finalize",
                        BindingFlags.NonPublic |
                        BindingFlags.Instance |
                        BindingFlags.DeclaredOnly);
            if (method is null)
            {
                return HasFinalizer(type.BaseType);
            }
            else
            {
                return method.GetBaseDefinition() == typeof(object).GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
    }
}
