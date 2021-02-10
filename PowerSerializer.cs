using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using DotNetCross.Memory;
using System.Runtime.Serialization;

namespace DouglasDwyer.PowerSerializer
{
    public class PowerSerializer
    {
        private MD5 TypeHasher = MD5.Create();
        private Dictionary<Type, Guid> IDTypeBinding = new Dictionary<Type, Guid>();
        private Dictionary<Guid, Type> TypeIDBinding = new Dictionary<Guid, Type>();

        private static MethodInfo MutateBoxedTypeGenericInfo = typeof(PowerSerializer).GetMethod("MutateValueType", BindingFlags.NonPublic | BindingFlags.Static);

        public PowerSerializer()
        {
            TypeIDBinding = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).ToDictionary(x => new Guid(TypeHasher.ComputeHash(Encoding.ASCII.GetBytes(x.AssemblyQualifiedName))));
            IDTypeBinding = TypeIDBinding.ToDictionary(x => x.Value, x => x.Key);
        }

        public virtual byte[] Serialize(object obj)
        {
            using(MemoryStream stream = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(stream))
                {
                    PowerSerializationContext context = new PowerSerializationContext();
                    LoadObjectGraph(context, writer, obj, true);
                    ImmutableList<Type> types = context.GetSerializedTypes();
                    writer.Write((ushort)types.Count);
                    foreach (Type type in types)
                    {
                        WriteType(writer, type);
                    }
                    ImmutableList<PowerSerializationContext.SerializedObjectData> data = context.GetSerializationData();
                    writer.Write((ushort)data.Count);
                    foreach (PowerSerializationContext.SerializedObjectData objData in data)
                    {
                        WriteCreationData(context, writer, objData);
                    }
                    foreach (PowerSerializationContext.SerializedObjectData objData in data)
                    {
                        WriteInitializationData(context, writer, objData.Value, objData.ObjectType);
                    }
                    return stream.ToArray();
                }
            }
        }

        public virtual object Deserialize(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    Type[] knownTypes = new Type[reader.ReadUInt16()];
                    for (int i = 0; i < knownTypes.Length; i++)
                    {
                        knownTypes[i] = ReadType(reader);
                    }
                    Type[] typeList = new Type[reader.ReadUInt16() + 1];
                    object[] objectGraph = new object[typeList.Length];
                    for(int i = 1; i < objectGraph.Length; i++)
                    {
                        (object, Type) createdObject = ReadCreationData(reader, knownTypes);
                        typeList[i] = createdObject.Item2;
                        objectGraph[i] = createdObject.Item1;
                    }
                    for (int i = 1; i < objectGraph.Length; i++)
                    {
                        ReadInitializationData(reader, objectGraph, typeList[i], objectGraph[i]);
                    }
                    return objectGraph[1];
                }
            }
        }

        public virtual T Deserialize<T>(byte[] data)
        {
            return (T)Deserialize(data);
        }

        protected virtual void SerializeObject(PowerSerializationContext context, BinaryWriter writer, object obj)
        {
            Type type = obj.GetType();
            context.AddType(type);
            context.RegisterObject(type, obj);
            if (type.IsArray)
            {
                //array behavior
                Array array = (Array)obj;
                byte rankSize = (byte)array.Rank;
                writer.Write(rankSize);
                int[] ranks = new int[rankSize];
                for(int i = 0; i < rankSize; i++)
                {
                    ranks[i] = array.GetLength(i);
                }
                Type elementType = type.GetElementType();
                if (elementType.IsPrimitive)
                {
                    WriteFlattenedPrimitiveArray(context, writer, array, 0, ranks, new int[rankSize]);
                }
                else if (elementType.IsValueType)
                {
                    WriteFlattenedValueTypeArray(context, writer, array, 0, ranks, new int[rankSize], elementType);
                }
                else
                {
                    WriteFlattenedArray(context, writer, array, 0, ranks, new int[rankSize]);
                }
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
                    WritePrimitiveObject(writer, info.GetValue(obj));
                }
                else if (info.FieldType.IsValueType)
                {
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
                        if (context.HasObject(obj))
                        {
                            writer.Write(context.GetDataForObject(obj).ID);
                        }
                        else
                        {
                            writer.Write(ushort.MaxValue);
                            SerializeObject(context, writer, value);
                        }
                    }
                }
            }
        }

        protected virtual void LoadObjectGraph(PowerSerializationContext context, BinaryWriter writer, object obj, bool referenced)
        {
            Type type = obj.GetType();
            if (referenced)
            {
                context.AddType(type);
                context.RegisterObject(type, obj);
            }
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                if(elementType.IsPrimitive) { }
                if(type.GetElementType().IsValueType) {
                    foreach (object value in (Array)obj)
                    {
                        if (value != null && !context.HasObject(value))
                        {
                            LoadObjectGraph(context, writer, value, false);
                        }
                    }
                }
                else
                {
                    foreach (object value in (Array)obj)
                    {
                        if (value != null && !context.HasObject(value))
                        {
                            LoadObjectGraph(context, writer, value, true);
                        }
                    }
                }
            }
            else
            {
                foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!info.FieldType.IsPrimitive)
                    {
                        object value = info.GetValue(obj);
                        if (value != null && !context.HasObject(value))
                        {
                            LoadObjectGraph(context, writer, value, !info.FieldType.IsValueType);
                        }
                    }
                }
            }
        }

        protected virtual void WriteCreationData(PowerSerializationContext context, BinaryWriter writer, PowerSerializationContext.SerializedObjectData data)
        {
            writer.Write(context.GetIDForType(data.ObjectType));
            if (data.ObjectType == typeof(string))
            {
                writer.Write((string)data.Value);
            }
            else if(data.ObjectType.IsArray)
            {
                Array array = (Array)data.Value;
                byte rankCount = (byte)array.Rank;
                writer.Write(rankCount);
                for (int i = 0; i < rankCount; i++)
                {
                    writer.Write(array.GetLength(i));
                }
            }
        }

        protected virtual void WriteInitializationData(PowerSerializationContext context, BinaryWriter writer, object obj, Type type)
        {
            if (type != typeof(string))
            {
                if (type.IsArray)
                {
                    Array array = (Array)obj;
                    byte rankCount = (byte)array.Rank;
                    int[] ranks = new int[rankCount];
                    for (int i = 0; i < rankCount; i++)
                    {
                        ranks[i] = array.GetLength(i);
                    }
                    Type elementType = type.GetElementType();
                    if (elementType.IsPrimitive)
                    {
                        WriteFlattenedPrimitiveArray(context, writer, array, 0, ranks, new int[rankCount]);
                    }
                    else if (elementType.IsValueType)
                    {
                        WriteFlattenedValueTypeArray(context, writer, array, 0, ranks, new int[rankCount], elementType);
                    }
                    else
                    {
                        WriteFlattenedArray(context, writer, array, 0, ranks, new int[rankCount]);
                    }
                }
                else if (type.IsPrimitive)
                {
                    WritePrimitiveObject(writer, obj);
                }
                else
                {
                    WriteReferenceTypeInitializationData(context, writer, obj, type);
                }
            }
        }

        private void WriteReferenceTypeInitializationData(PowerSerializationContext context, BinaryWriter writer, object obj, Type type)
        {
            foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object fieldValue = info.GetValue(obj);
                if (info.FieldType.IsValueType)
                {
                    WriteInitializationData(context, writer, fieldValue, info.FieldType);
                }
                else
                {
                    if (fieldValue is null)
                    {
                        writer.Write((ushort)0);
                    }
                    else
                    {
                        writer.Write(context.GetDataForObject(fieldValue).ID);
                    }
                }
            }
        }

        protected virtual (object created, Type creationType) ReadCreationData(BinaryReader reader, Type[] types)
        {
            int v;
            Type type = types[v = reader.ReadUInt16()];
            if (type == typeof(string))
            {
                return (reader.ReadString(), type);
            }
            else if(type.IsArray)
            {
                byte rankCount = reader.ReadByte();
                int[] ranks = new int[rankCount];
                for(int i = 0; i < ranks.Length; i++)
                {
                    ranks[i] = reader.ReadInt32();
                }
                return (Array.CreateInstance(type.GetElementType(), ranks), type);
            }
            else
            {
                return (FormatterServices.GetUninitializedObject(type), type);
            }
        }

        protected virtual void ReadInitializationData(BinaryReader reader, object[] objectGraph, Type type, object obj)
        {
            if(type != typeof(string))
            {
                if (type.IsArray)
                {
                    Array array = (Array)obj;
                    byte rankCount = (byte)array.Rank;
                    int[] ranks = new int[rankCount];
                    for (int i = 0; i < rankCount; i++)
                    {
                        ranks[i] = array.GetLength(i);
                    }
                    Type elementType = type.GetElementType();
                    if (elementType.IsPrimitive)
                    {
                        ReadFlattenedPrimitiveArray(reader, array, 0, ranks, new int[rankCount], elementType);
                    }
                    else if (elementType.IsValueType)
                    {
                        ReadFlattenedValueTypeArray(reader, objectGraph, array, 0, ranks, new int[rankCount], elementType);
                    }
                    else
                    {
                        ReadFlattenedArray(reader, objectGraph, array, 0, ranks, new int[rankCount]);
                    }
                }
                else if (type.IsPrimitive)
                {
                    MutateBoxedType(obj, ReadPrimitiveObject(reader, type), type);
                }
                else
                {
                    ReadReferenceTypeInitializationData(reader, objectGraph, obj, type);
                }
            }
        }

        private void ReadReferenceTypeInitializationData(BinaryReader reader, object[] objectGraph, object obj, Type type)
        {
            foreach (FieldInfo info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if(info.FieldType.IsPrimitive)
                {
                    info.SetValue(obj, ReadPrimitiveObject(reader, info.FieldType));
                }
                else if (info.FieldType.IsValueType)
                {
                    object fieldValue = FormatterServices.GetUninitializedObject(info.FieldType);
                    ReadInitializationData(reader, objectGraph, info.FieldType, fieldValue);
                    info.SetValue(obj, fieldValue);
                }
                else
                {
                    info.SetValue(obj, objectGraph[reader.ReadUInt16()]);
                }
            }
        }

        protected virtual void WriteType(BinaryWriter writer, Type type)
        {
            if(type.IsArray)
            {
                WriteType(writer, typeof(SerializedArray<>).MakeGenericType(new[] { type.GetElementType() }));
            }
            else if(type.IsGenericType)
            {
                writer.Write(IDTypeBinding[type.GetGenericTypeDefinition()].ToByteArray());
                foreach(Type arg in type.GenericTypeArguments)
                {
                    WriteType(writer, arg);
                }
            }
            else
            {
                writer.Write(IDTypeBinding[type].ToByteArray());
            }
        }

        protected virtual Type ReadType(BinaryReader reader)
        {
            Type baseType = TypeIDBinding[new Guid(reader.ReadBytes(16))];
            if(baseType.IsGenericType)
            {
                Type[] typeArguments = new Type[baseType.GetGenericArguments().Length];
                for(int i = 0; i < typeArguments.Length; i++)
                {
                    typeArguments[i] = ReadType(reader);
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
                object value = array.GetValue(currentRanks);
                if (value is null)
                {
                    writer.Write((ushort)0);
                }
                else
                {
                    writer.Write(context.GetDataForObject(value).ID);
                }
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
                WriteInitializationData(context, writer, array.GetValue(currentRanks), type);
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

        private void ReadFlattenedArray(BinaryReader reader, object[] objectGraph, Array array, int index, int[] ranks, int[] currentRanks)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    ReadFlattenedArray(reader, objectGraph, array, index + 1, ranks, currentRanks);
                    currentRanks[index]++;
                }
            }
            else
            {
                array.SetValue(objectGraph[reader.ReadUInt16()], currentRanks);
            }
        }

        private void ReadFlattenedValueTypeArray(BinaryReader reader, object[] objectGraph, Array array, int index, int[] ranks, int[] currentRanks, Type type)
        {
            if (index < ranks.Length)
            {
                for (int i = 0; i < ranks[index]; i++)
                {
                    ReadFlattenedValueTypeArray(reader, objectGraph, array, index + 1, ranks, currentRanks, type);
                    currentRanks[index]++;
                }
            }
            else
            {
                object obj = FormatterServices.GetUninitializedObject(type);
                ReadInitializationData(reader, objectGraph, type, obj);
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

        private void WritePrimitiveObject(BinaryWriter writer, object primitive)
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

        private object ReadPrimitiveObject(BinaryReader reader, Type type)
        {
            if (type == typeof(byte))
            {
                return reader.Read();
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

        private static void MutateBoxedType(object obj, object replacement, Type type)
        {
            MutateBoxedTypeGenericInfo.MakeGenericMethod(new[] { type }).Invoke(null, new[] { obj, replacement });
        }

        private static void MutateValueType<T>(object obj, T replacement) where T : struct
        {
            ref T value = ref Unsafe.Unbox<T>(obj);
            value = replacement;
        }

        private class SerializedArray<T> { }
    }
}
