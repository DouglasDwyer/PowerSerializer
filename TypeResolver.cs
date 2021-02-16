using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Types that implement <see cref="ITypeResolver"/> mediate which types can be serialized/deserialized by a <see cref="PowerSerializer"/>. Additionally, they each provide a way to identify types across serialization boundaries.
    /// </summary>
    public interface ITypeResolver
    {
        /// <summary>
        /// Returns whether the given type should be serialized or deserialized.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>Whether the type is serializable.</returns>
        bool IsTypeSerializable(Type type);
        /// <summary>
        /// Writes a binary representation of the given type's identity to the given binary writer's stream.
        /// </summary>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="type">The type whose ID should be written.</param>
        void WriteTypeID(BinaryWriter writer, Type type);
        /// <summary>
        /// Reads the binary representation of a type's identity from the given binary reader's stream and returns the type.
        /// </summary>
        /// <param name="reader">The binary reader from which to read.</param>
        /// <returns>The type whose ID was written to the reader's stream.</returns>
        Type ReadTypeID(BinaryReader reader);
    }
}
