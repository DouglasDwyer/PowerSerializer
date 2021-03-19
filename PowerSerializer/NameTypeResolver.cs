using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="NameTypeResolver"/> utilize the a type's assembly-qualified name in order to resolve it across serialization boundaries. They allow for serialization of all types.
    /// </summary>
    public class NameTypeResolver : ITypeResolver
    {
        /// <summary>
        /// Returns whether the given type should be serialized or deserialized.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>Whether the type is serializable.</returns>
        public virtual bool IsTypeSerializable(Type type)
        {
            return true;
        }

        /// <summary>
        /// Reads the binary representation of a type's identity from the given binary reader's stream and returns the type.
        /// </summary>
        /// <param name="reader">The binary reader from which to read.</param>
        /// <returns>The type whose ID was written to the reader's stream.</returns>
        public virtual Type ReadTypeID(BinaryReader reader)
        {
            return Type.GetType(reader.ReadString());
        }

        /// <summary>
        /// Writes a binary representation of the given type's identity to the given binary writer's stream.
        /// </summary>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="type">The type whose ID should be written.</param>
        public virtual void WriteTypeID(BinaryWriter writer, Type type)
        {
            writer.Write(type.AssemblyQualifiedName);
        }
    }
}
