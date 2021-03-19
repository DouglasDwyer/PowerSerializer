using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Instances of <see cref="GuidTypeResolver"/> utilize the MD5 hash of a type's name in order to resolve it across serialization boundaries. They allow for serialization of any type specified in the constructor.
    /// </summary>
    public class GuidTypeResolver : ITypeResolver
    {
        private static MD5 TypeHasher = MD5.Create();
        private ImmutableDictionary<Type, Guid> IDTypeBinding;
        private ImmutableDictionary<Guid, Type> TypeIDBinding;

        /// <summary>
        /// Creates a new resolver instance with the given type list. The resolver will only allow serialization of those specified types.
        /// </summary>
        /// <param name="allowedTypes">The types to allow during serialization.</param>
        public GuidTypeResolver(IEnumerable<Type> allowedTypes)
        {
            TypeIDBinding = allowedTypes.Concat(new[] { typeof(SerializedArray<>), typeof(NullRepresentative) }).Distinct().ToImmutableDictionary(x => new Guid(TypeHasher.ComputeHash(Encoding.ASCII.GetBytes(GetTypeName(x)))));
            IDTypeBinding = TypeIDBinding.ToImmutableDictionary(x => x.Value, x => x.Key);
        }

        /// <summary>
        /// Returns whether the given type should be serialized or deserialized.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>Whether the type is serializable.</returns>
        public virtual bool IsTypeSerializable(Type type)
        {
            if(type.IsArray)
            {
                return IsTypeSerializable(type.GetElementType());
            }
            else if(type.IsGenericType)
            {
                return IDTypeBinding.ContainsKey(type.GetGenericTypeDefinition()) || IDTypeBinding.ContainsKey(type);
            }
            else
            {
                return IDTypeBinding.ContainsKey(type);
            }
        }

        /// <summary>
        /// Writes a binary representation of the given type's identity to the given binary writer's stream.
        /// </summary>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="type">The type whose ID should be written.</param>
        public virtual void WriteTypeID(BinaryWriter writer, Type type)
        {
            if (type.IsArray)
            {
                WriteTypeID(writer, typeof(SerializedArray<>).MakeGenericType(new[] { type.GetElementType() }));
            }
            else if (type.IsGenericType)
            {
                writer.Write(IDTypeBinding[type.GetGenericTypeDefinition()].ToByteArray());
                foreach (Type arg in type.GenericTypeArguments)
                {
                    WriteTypeID(writer, arg);
                }
            }
            else
            {
                writer.Write(IDTypeBinding[type].ToByteArray());
            }
        }

        /// <summary>
        /// Reads the binary representation of a type's identity from the given binary reader's stream and returns the type.
        /// </summary>
        /// <param name="reader">The binary reader from which to read.</param>
        /// <returns>The type whose ID was written to the reader's stream.</returns>
        public virtual Type ReadTypeID(BinaryReader reader)
        {
            Type baseType = TypeIDBinding[new Guid(reader.ReadBytes(16))];
            if (baseType.IsGenericType)
            {
                Type[] typeArguments = new Type[baseType.GetGenericArguments().Length];
                for (int i = 0; i < typeArguments.Length; i++)
                {
                    typeArguments[i] = ReadTypeID(reader);
                }
                if (baseType == typeof(SerializedArray<>))
                {
                    return typeArguments[0].MakeArrayType();
                }
                else
                {
                    return baseType.MakeGenericType(typeArguments);
                }
            }
            else
            {
                return baseType;
            }
        }

        /// <summary>
        /// Returns a string identifying the given type. By default, this returns the assembly-qualified name, but this can be overriden to allow for serialization between different assemblies.
        /// </summary>
        /// <param name="type">The type to examine.</param>
        /// <returns>A name that uniquely identifies the type.</returns>
        protected virtual string GetTypeName(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        private sealed class SerializedArray<T> { }
    }
}
