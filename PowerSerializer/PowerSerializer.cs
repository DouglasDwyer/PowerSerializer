using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;

namespace DouglasDwyer.PowerSerializer
{
    /// <summary>
    /// Allows for customizable binary serialization and deserialization of complex types. Instances of <see cref="PowerSerializer"/> can serialize absolutely anything, provided that their type resolver allows it.
    /// </summary>
    public class PowerSerializer : ICloneable
    {
        /// <summary>
        /// The resolver used to identify/verify serialized types.
        /// </summary>
        public ITypeResolver TypeResolver { get; }

        /// <summary>
        /// Creates a new serializer instance with a <see cref="FinalizerLimitedTypeResolver"/> limited to types without finalizers.
        /// </summary>
        public PowerSerializer() : this(new FinalizerLimitedTypeResolver()) { }

        /// <summary>
        /// Creates a new serializer instance with the specified type resolver.
        /// </summary>
        /// <param name="resolver">The type resolver that should be used during serialization/deseriation for identifing/verifying types.</param>
        public PowerSerializer(ITypeResolver resolver)
        {
            TypeResolver = resolver;
        }

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>A byte-based representation of the object.</returns>
        public virtual byte[] Serialize(object obj)
        {
            if(obj is null)
            {
                obj = new NullRepresentative();
            }

            using(MemoryStream stream = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    PowerSerializationContext context = CreateSerializationContext();
                    Type objType = context.RegisterObject(obj).Item2;
                    CheckTypeAllowance(objType);
                    if (objType == typeof(string))
                    {
                        writer.Write((string)obj);
                        long pos = writer.BaseStream.Position;
                        TypeResolver.WriteTypeID(writer, objType);
                        writer.Write((int)(writer.BaseStream.Position - pos));
                    }
                    else if(obj is Type typeRepresentation)
                    {
                        TypeResolver.WriteTypeID(writer, typeRepresentation);
                        long pos = writer.BaseStream.Position;
                        TypeResolver.WriteTypeID(writer, typeof(Type));
                        writer.Write((int)(writer.BaseStream.Position - pos));
                    }
                    else if(objType.IsPrimitive)
                    {
                        WritePrimitiveObject(writer, obj);
                        long pos = writer.BaseStream.Position;
                        TypeResolver.WriteTypeID(writer, objType);
                        writer.Write((int)(writer.BaseStream.Position - pos));
                    }
                    else
                    {
                        if (obj is Array array)
                        {
                            byte rankSize = (byte)array.Rank;
                            writer.Write(rankSize);
                            for (int i = 0; i < array.Rank; i++)
                            {
                                writer.Write(array.GetLength(i));
                            }
                        }
                        for (int i = 1; i < context.ObjectGraph.Count; i++)
                        {
                            SerializeObject(context, writer, context.ObjectGraph[i], context.ObjectGraph[i].GetType());
                        }
                        ImmutableList<Type> types = context.IncludedTypes;
                        long pos = writer.BaseStream.Position;
                        foreach (Type type in types)
                        {
                            TypeResolver.WriteTypeID(writer, type);
                        }
                        writer.Write((int)(writer.BaseStream.Position - pos));
                    }
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Deserializes an object from a byte array.
        /// </summary>
        /// <param name="data">The byte-based representation of the object.</param>
        /// <returns>The deserialized object.</returns>
        public virtual object Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    PowerDeserializationContext context = CreateDeserializationContext();
                    int dataLength = data.Length - 4;
                    stream.Position = dataLength;
                    int typeSize = reader.ReadInt32();
                    stream.Position = data.Length - typeSize - 4;

                    List<Type> knownTypes = new List<Type>();
                    while (reader.BaseStream.Position < dataLength)
                    {
                        knownTypes.Add(TypeResolver.ReadTypeID(reader));
                    }
                    stream.Position = 0;
                    context.IncludedTypes = knownTypes;

                    object obj = ReadAndCreateObject(context, reader, knownTypes[0]);
                    if(obj is NullRepresentative)
                    {
                        obj = null;
                    }
                    for (int i = 1; i < context.ObjectGraph.Count; i++)
                    {
                        DeserializeObject(context, reader, context.ObjectGraph[i], context.ObjectGraph[i].GetType());
                    }

                    return ProcessObjectGraph(context);
                }
            }
        }

        /// <summary>
        /// Deserializes an object from a byte array.
        /// </summary>
        /// <typeparam name="T">The type of the deserialized object.</typeparam>
        /// <param name="data">The byte-based representation of the object.</param>
        /// <returns>The deserialized object.</returns>
        public virtual T Deserialize<T>(byte[] data)
        {
            return (T)Deserialize(data);
        }

        /// <summary>
        /// Creates a new serialization context for storing data about a serialization operation.
        /// </summary>
        /// <returns>The new serialization context.</returns>
        protected virtual PowerSerializationContext CreateSerializationContext()
        {
            return new PowerSerializationContext();
        }

        /// <summary>
        /// Creates a new deserialization context for storing data about a deserialization operation.
        /// </summary>
        /// <returns></returns>
        protected virtual PowerDeserializationContext CreateDeserializationContext()
        {
            return new PowerDeserializationContext();
        }

        /// <summary>
        /// Processes the generated object graph, makes any last-minute changes if necessary, and returns the deserialized object.
        /// </summary>
        /// <param name="context">The current deserialization context.</param>
        /// <returns>The final deserialized object.</returns>
        protected virtual object ProcessObjectGraph(PowerDeserializationContext context)
        {
            return context.ObjectGraph[1];
        }

        /// <summary>
        /// Serializes the given object, writing its object references and primitive data to the given binary writer. Referenced objects are not serialized, but are added to the object graph.
        /// </summary>
        /// <param name="context">The current serialization context, with data about the current object graph and known types.</param>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="obj">The object that should be serialized.</param>
        /// <param name="type">The type of the current object.</param>
        protected virtual void SerializeObject(PowerSerializationContext context, BinaryWriter writer, object obj, Type type)
        {
            if (type.IsArray)
            {
                Array array = (Array)obj;
                int[] ranks = new int[array.Rank];
                for(int i = 0; i < array.Rank; i++)
                {
                    ranks[i] = array.GetLength(i);
                }
                Type elementType = type.GetElementType();
                CheckTypeAllowance(type);
                if (elementType.IsPrimitive)
                {
                    WriteFlattenedPrimitiveArray(context, writer, array, 0, ranks, new int[array.Rank]);
                }
                else if (elementType.IsValueType)
                {
                    WriteFlattenedValueTypeArray(context, writer, array, 0, ranks, new int[array.Rank], elementType);
                }
                else
                {
                    WriteFlattenedArray(context, writer, array, 0, ranks, new int[array.Rank]);
                }
            }
            else if(type.IsPrimitive)
            {
                
            }
            else
            {
                SerializeReferenceType(context, writer, obj, type);
            }
        }

        private void SerializeReferenceType(PowerSerializationContext context, BinaryWriter writer, object obj, Type type)
        {
            foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (info.FieldType.IsPrimitive)
                {
                    CheckTypeAllowance(info.FieldType);
                    WritePrimitiveObject(writer, info.GetValue(obj));
                }
                else if (info.FieldType.IsValueType)
                {
                    CheckTypeAllowance(info.FieldType);
                    SerializeReferenceType(context, writer, info.GetValue(obj), info.FieldType);
                }
                else
                {
                    object value = info.GetValue(obj);
                    if (value is null)
                    {
                        writer.Write((ushort)0);
                    }
                    else
                    {
                        CheckTypeAllowance(value.GetType());
                        WriteObjectReference(context, writer, value);
                    }
                }
            }
        }

        /// <summary>
        /// Writes an object reference to the given binary writer. If the object has not been referenced before, its type is also stored, along with any immutable type data (like the contents of a string).
        /// </summary>
        /// <param name="context">The current serialization context, with data about the current object graph and known types.</param>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="obj">The object being referenced.</param>
        protected virtual void WriteObjectReference(PowerSerializationContext context, BinaryWriter writer, object obj)
        {
            if(obj is null)
            {
                writer.Write((ushort)0);
            }
            else if (context.HasObject(obj))
            {
                writer.Write(context.GetObjectID(obj));
            }
            else if (obj is Type typeRepresentation)
            {
                (ushort, Type) objectData = context.RegisterObject(obj);
                writer.Write(objectData.Item1);
                writer.Write(context.GetTypeID(typeof(Type)));
                TypeResolver.WriteTypeID(writer, typeRepresentation);                
            }
            else
            {
                (ushort, Type) objectData = context.RegisterObject(obj);
                writer.Write(objectData.Item1);
                writer.Write(context.GetTypeID(objectData.Item2));
                if(obj is string oString)
                {
                    writer.Write(oString);
                }
                else if(objectData.Item2.IsPrimitive)
                {
                    WritePrimitiveObject(writer, obj);
                }
                else if(obj is Array array)
                {
                    byte rankSize = (byte)array.Rank;
                    writer.Write(rankSize);
                    for (int i = 0; i < array.Rank; i++)
                    {
                        writer.Write(array.GetLength(i));
                    }
                }
            }
        }

        /// <summary>
        /// Deserializes the given object, populating its fields by reading its object references and primitive data from the given binary reader.
        /// </summary>
        /// <param name="context">The current deserialization context, with data about the current object graph and known types.</param>
        /// <param name="reader">The binary reader to utilize.</param>
        /// <param name="obj">The object whose fields should be populated.</param>
        /// <param name="type">The type of the deserialized object.</param>
        protected virtual void DeserializeObject(PowerDeserializationContext context, BinaryReader reader, object obj, Type type)
        {
            if (type.IsArray)
            {
                Array array = (Array)obj;
                int[] ranks = new int[array.Rank];
                for (int i = 0; i < array.Rank; i++)
                {
                    ranks[i] = array.GetLength(i);
                }
                Type elementType = type.GetElementType();
                CheckTypeAllowance(elementType);
                if (elementType.IsPrimitive)
                {
                    ReadFlattenedPrimitiveArray(reader, array, 0, ranks, new int[array.Rank], elementType);
                }
                else if (elementType.IsValueType)
                {
                    ReadFlattenedValueTypeArray(context, reader, array, 0, ranks, new int[array.Rank], elementType);
                }
                else
                {
                    ReadFlattenedArray(context, reader, array, 0, ranks, new int[array.Rank]);
                }
            }
            else if(type.IsPrimitive || obj is Type) { }
            else
            {
                DeserializeReferenceType(context, reader, obj, type);
            }
        }

        private void DeserializeReferenceType(PowerDeserializationContext context, BinaryReader reader, object obj, Type type)
        {
            foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (info.FieldType.IsPrimitive)
                {
                    CheckTypeAllowance(info.FieldType);
                    info.SetValue(obj, ReadPrimitiveObject(reader, info.FieldType));
                }
                else if (info.FieldType.IsValueType)
                {
                    CheckTypeAllowance(info.FieldType);
                    object field = FormatterServices.GetUninitializedObject(info.FieldType);
                    DeserializeReferenceType(context, reader, field, info.FieldType);
                    info.SetValue(obj, field);
                }
                else
                {
                    info.SetValue(obj, ReadObjectReference(context, reader));
                }
            }
        }

        /// <summary>
        /// Reads an object reference from the given binary reader. If the object has not been referenced before, a new object of the specified type is instantiated.
        /// </summary>
        /// <param name="context">The current deserialization context, with data about the current object graph and known types.</param>
        /// <param name="reader">The binary reader to utilize.</param>
        /// <returns>The referenced object.</returns>
        protected virtual object ReadObjectReference(PowerDeserializationContext context, BinaryReader reader)
        {
            ushort id = reader.ReadUInt16();
            if(id == 0)
            {
                return null;
            }
            else if (context.HasObject(id))
            {
                return context.GetObject(id);
            }
            else
            {
                return ReadAndCreateObject(context, reader, context.GetTypeFromID(reader.ReadUInt16()));
            }
        }

        /// <summary>
        /// Reads an object's data from the specified binary reader and creates a new instance of that object with the given data. For user-defined types, a new object of the specified type is returned without any fields initialized. For arrays, a new object with the correct rank lengths is returned. For strings and primitives, an object with the correct value is returned.
        /// </summary>
        /// <param name="context">The current deserialization context, with data about the current object graph and known types.</param>
        /// <param name="reader">The binary reader to utilize.</param>
        /// <param name="type">The type of the object to create.</param>
        /// <returns>The created object.</returns>
        protected virtual object ReadAndCreateObject(PowerDeserializationContext context, BinaryReader reader, Type type)
        {
            CheckTypeAllowance(type);
            object toReturn;
            if (type == typeof(string))
            {
                toReturn = reader.ReadString();
            }
            else if(type == typeof(Type))
            {
                toReturn = TypeResolver.ReadTypeID(reader);
            }
            else if (type.IsPrimitive)
            {
                toReturn = ReadPrimitiveObject(reader, type);
            }
            else if (type.IsArray)
            {
                byte rankSize = reader.ReadByte();
                int[] ranks = new int[rankSize];
                for (int i = 0; i < rankSize; i++)
                {
                    ranks[i] = reader.ReadInt32();
                }
                toReturn = Array.CreateInstance(type.GetElementType(), ranks);
            }
            else
            {
                toReturn = FormatterServices.GetUninitializedObject(type);
            }
            context.RegisterNextObject(toReturn);
            return toReturn;
        }

        private void WriteFlattenedArray(PowerSerializationContext context, BinaryWriter writer, Array array, int index, int[] ranks, int[] currentRanks)
        {
            if(index < ranks.Length)
            {
                for(int i = 0; i < ranks[index]; i++)
                {
                    WriteFlattenedArray(context, writer, array, index + 1, ranks, currentRanks);
                    currentRanks[index]++;
                }
            }
            else
            {
                WriteObjectReference(context, writer, array.GetValue(currentRanks));
            }
        }

        private void WriteFlattenedValueTypeArray(PowerSerializationContext context, BinaryWriter writer, Array array, int index, int[] ranks, int[] currentRanks, Type type)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    WriteFlattenedValueTypeArray(context, writer, array, index + 1, ranks, currentRanks, type);
                    currentRanks[index]++;
                }
            }
            else
            {
                SerializeReferenceType(context, writer, array.GetValue(currentRanks), type);
            }
        }

        private void WriteFlattenedPrimitiveArray(PowerSerializationContext context, BinaryWriter writer, Array array, int index, int[] ranks, int[] currentRanks)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    WriteFlattenedPrimitiveArray(context, writer, array, index + 1, ranks, currentRanks);
                    currentRanks[index]++;
                }
            }
            else
            {
                WritePrimitiveObject(writer, array.GetValue(currentRanks));
            }
        }

        private void ReadFlattenedArray(PowerDeserializationContext context, BinaryReader reader, Array array, int index, int[] ranks, int[] currentRanks)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    ReadFlattenedArray(context, reader, array, index + 1, ranks, currentRanks);
                    currentRanks[index]++;
                }
            }
            else
            {
                array.SetValue(ReadObjectReference(context, reader), currentRanks);
            }
        }

        private void ReadFlattenedValueTypeArray(PowerDeserializationContext context, BinaryReader reader, Array array, int index, int[] ranks, int[] currentRanks, Type type)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    ReadFlattenedValueTypeArray(context, reader, array, index + 1, ranks, currentRanks, type);
                    currentRanks[index]++;
                }
            }
            else
            {
                object obj = FormatterServices.GetUninitializedObject(type);
                DeserializeReferenceType(context, reader, obj, type);
                array.SetValue(obj, currentRanks);
            }
        }

        private void ReadFlattenedPrimitiveArray(BinaryReader reader, Array array, int index, int[] ranks, int[] currentRanks, Type type)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    ReadFlattenedPrimitiveArray(reader, array, index + 1, ranks, currentRanks, type);
                    currentRanks[index]++;
                }
            }
            else
            {
                array.SetValue(ReadPrimitiveObject(reader, type), currentRanks);
            }
        }

        /// <summary>
        /// Writes the value of a primitive object to the writer.
        /// </summary>
        /// <param name="writer">The binary writer to utilize.</param>
        /// <param name="primitive">The object to write.</param>
        protected virtual void WritePrimitiveObject(BinaryWriter writer, object primitive)
        {
            if (primitive is byte pByte)
            {
                writer.Write(pByte);
            }
            else if (primitive is short pShort)
            {
                writer.Write(pShort);
            }
            else if (primitive is int pInt)
            {
                writer.Write(pInt);
            }
            else if (primitive is long pLong)
            {
                writer.Write(pLong);
            }
            else if (primitive is sbyte pSbyte)
            {
                writer.Write(pSbyte);
            }
            else if (primitive is ushort pUshort)
            {
                writer.Write(pUshort);
            }
            else if (primitive is uint pUint)
            {
                writer.Write(pUint);
            }
            else if (primitive is ulong pUlong)
            {
                writer.Write(pUlong);
            }
            else if (primitive is bool pBool)
            {
                writer.Write(pBool);
            }
            else if (primitive is float pFloat)
            {
                writer.Write(pFloat);
            }
            else if (primitive is double pDouble)
            {
                writer.Write(pDouble);
            }
            else if (primitive is decimal pDecimal)
            {
                writer.Write(pDecimal);
            }
            else if (primitive is char pChar)
            {
                writer.Write(pChar);
            }
            else if(primitive is IntPtr pIntPtr)
            {
                writer.Write(pIntPtr.ToInt64());
            }
            else if(primitive is UIntPtr pUIntPtr)
            {
                writer.Write(pUIntPtr.ToUInt64());
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Reads the value of a primitive object from the reader.
        /// </summary>
        /// <param name="reader">The binary reader to utilize.</param>
        /// <param name="type">The type of the primitive to read.</param>
        /// <returns>The primitive value.</returns>
        protected virtual object ReadPrimitiveObject(BinaryReader reader, Type type)
        {
            if (type == typeof(byte))
            {
                return reader.ReadByte();
            }
            else if (type == typeof(short))
            {
                return reader.ReadInt16();
            }
            else if (type == typeof(int))
            {
                return reader.ReadInt32();
            }
            else if (type == typeof(long))
            {
                return reader.ReadInt64();
            }
            else if (type == typeof(sbyte))
            {
                return reader.ReadSByte();
            }
            else if (type == typeof(ushort))
            {
                return reader.ReadUInt16();
            }
            else if (type == typeof(uint))
            {
                return reader.ReadUInt32();
            }
            else if (type == typeof(ulong))
            {
                return reader.ReadUInt64();
            }
            else if (type == typeof(bool))
            {
                return reader.ReadBoolean();
            }
            else if (type == typeof(float))
            {
                return reader.ReadSingle();
            }
            else if (type == typeof(double))
            {
                return reader.ReadDouble();
            }
            else if (type == typeof(decimal))
            {
                return reader.ReadDecimal();
            }
            else if (type == typeof(char))
            {
                return reader.ReadChar();
            }
            else if (type == typeof(IntPtr))
            {
                if(IntPtr.Size == 8)
                {
                    return new IntPtr(reader.ReadInt64());
                }
                else
                {
                    return new IntPtr(reader.ReadInt32());
                }
            }
            else if (type == typeof(UIntPtr))
            {
                if (IntPtr.Size == 8)
                {
                    return new UIntPtr(reader.ReadUInt64());
                }
                else
                {
                    return new UIntPtr(reader.ReadUInt32());
                }
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Checks whether serialization of a type is allowed under the current type resovler, and throws an exception if serialization is forbidden.
        /// </summary>
        /// <param name="type">The type to check.</param>
        protected virtual void CheckTypeAllowance(Type type)
        {
            if (!TypeResolver.IsTypeSerializable(type))
            {
                throw new SerializationException("The current type resolver does not allow for serialization/deserialization of " + type + ".");
            }
        }

        /// <summary>
        /// Creates a memberwise copy of this object, returning a <see cref="PowerSerializer"/> with the same settings as the original.
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }
    }
}
