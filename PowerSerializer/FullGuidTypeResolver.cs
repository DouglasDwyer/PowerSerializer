using System;
using System.Collections.Generic;
using System.Linq;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="FullGuidTypeResolver"/> allow for serialization of any and all types loaded in the current <see cref="AppDomain"/> upon resolver construction. They utilize the MD5 hash of a type's name in order to resolve it across serialization boundaries.
    /// </summary>
    /// <remarks>Allowing deserialization of all types is dangerous unless the data to be deserialized is fully trusted. Attackers can submit deserialization payloads that generate objects in invalid states, or objects that may cause unwanted code execution during their finalization routines. Consider using a <see cref="ClassLimitedTypeResolver"/> or <see cref="FinalizerLimitedGuidTypeResolver"/> instead to minimize security risks.</remarks>
    [Obsolete("Allowing deserialization of all types is dangerous unless the data to be deserialized is fully trusted. Attackers can submit deserialization payloads that generate objects in invalid states, or objects that may cause unwanted code execution during their finalization routines. Consider using a class-limited or finalizer-limited serializer instead to minimize security risks.", false)]
    public class FullGuidTypeResolver : GuidTypeResolver
    {
        /// <summary>
        /// Creates a new resolver instance, allowing all types currently loaded in the <see cref="AppDomain"/> to be serialized.
        /// </summary>
        public FullGuidTypeResolver() : base(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()))
        {

        }
    }
}
