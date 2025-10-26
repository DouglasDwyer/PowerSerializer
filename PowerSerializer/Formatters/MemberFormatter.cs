using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

public static class Tester
{
    public static void AfterDeser(BufferReader reader, in int value)
    {
        reader.ReadInt32();
        Unsafe.AsRef(in value) = 22;
        Console.WriteLine("rage of stinky " + value);
    }
}

/// <summary>
/// Serializes a type by iterating over its individual members and serializing them.
/// </summary>
public class MemberFormatter<T> : IFormatter<T>
{
    private delegate void DeserializeDelegate(BufferReader reader, out T value);
    private delegate void SerializeDelegate(BufferWriter writer, in T value);

    private readonly DeserializeDelegate _deserialize;
    private readonly SerializeDelegate _serialize;

    public MemberFormatter(PowerSerializer serializer)
    {
        try
        {
            if (typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, []) is null)
            {
                throw new ArgumentException("Member-formatted types must have public parameterless constructor", nameof(T));
            }

            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            var entries = GetEntries(serializer, typeof(T), fields);

            _deserialize = CompileDeserializer(typeof(T), entries);
            _serialize = CompileSerializer(typeof(T), entries);
        }
        catch
        {
            Console.WriteLine("slag it");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        _deserialize(reader, out value);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        _serialize(writer, value);
    }

    private static DeserializeDelegate CompileDeserializer(Type type, ReadOnlySpan<FieldEntry> entries)
    {
        var readerParam = Expression.Parameter(typeof(BufferReader), "reader");
        var valueParam = Expression.Parameter(type.MakeByRefType(), "value");

        var bodyStatements = new Expression[1 + entries.Length];
        bodyStatements[0] = Expression.Assign(valueParam, Expression.New(type));

        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.Member.FieldType == typeof(int))
            {
                bodyStatements[i + 1] = Expression.Call(
                    null,
                    typeof(Tester).GetMethod("AfterDeser")!,
                    readerParam,
                    Expression.Field(valueParam, entry.Member));
            }
            else
            {
                bodyStatements[i + 1] = Expression.Call(
                    Expression.Constant(entry.Formatter),
                    GetDeserializeImplementation(entry.Formatter.GetType(), entry.Member.FieldType),
                    readerParam,
                    Expression.Field(valueParam, entry.Member));
            }
        }

        var body = Expression.Block(bodyStatements);
        var lambda = Expression.Lambda<DeserializeDelegate>(body, readerParam, valueParam);
        return lambda.Compile(true);
    }

    private static SerializeDelegate CompileSerializer(Type type, ReadOnlySpan<FieldEntry> entries)
    {
        var writerParam = Expression.Parameter(typeof(BufferWriter), "writer");
        var valueParam = Expression.Parameter(type.MakeByRefType(), "value");

        var bodyStatements = new Expression[entries.Length];
        
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            bodyStatements[i] = Expression.Call(
                Expression.Constant(entry.Formatter),
                GetSerializeImplementation(entry.Formatter.GetType(), entry.Member.FieldType),
                writerParam,
                Expression.Field(valueParam, entry.Member));
        }

        var body = Expression.Block(bodyStatements);
        var lambda = Expression.Lambda<SerializeDelegate>(body, writerParam, valueParam);
        return lambda.Compile(true);
    }

    private static FieldEntry[] GetEntries(PowerSerializer serializer, Type type, IEnumerable<FieldInfo> fields)
    {
        var inheritanceHierarchy = GetInheritanceHierarchy(type);
        var fieldsSorted = fields.Distinct().OrderBy(x => (inheritanceHierarchy.IndexOf(x.DeclaringType!), x.Name)).ToArray();
        var entries = new FieldEntry[fieldsSorted.Length];

        for (var i = 0; i < fieldsSorted.Length; i++)
        {
            var field = fieldsSorted[i];
            entries[i] = new FieldEntry
            {
                Formatter = serializer.GetFormatter(field.FieldType),
                Member = field
            };
        }

        return entries;
    }

    private static List<Type> GetInheritanceHierarchy(Type type)
    {
        var result = new List<Type>();
        var currentType = type;
        while (currentType != null)
        {
            result.Add(currentType);
            currentType = currentType.BaseType;
        }

        return result;
    }

    private static MethodInfo GetSerializeImplementation(Type formatterType, Type targetType)
    {
        // todo: cache some of this
        var formatterInterface = typeof(IFormatter<>).MakeGenericType(targetType);
        return GetImplementationMethod(formatterType, formatterInterface, formatterInterface.GetMethod("Serialize")!);
    }

    private static MethodInfo GetDeserializeImplementation(Type formatterType, Type targetType)
    {
        // todo: cache some of this
        var formatterInterface = typeof(IFormatter<>).MakeGenericType(targetType);
        return GetImplementationMethod(formatterType, formatterInterface, formatterInterface.GetMethod("Deserialize")!);
    }

    private static MethodInfo GetImplementationMethod(Type implementingClass, Type implementedInterface, MethodInfo interfaceMethod)
    {
        var map = implementingClass.GetInterfaceMap(implementedInterface);
        var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
        return map.TargetMethods[index];
    }

    /// <summary>
    /// Describes a specific field to be serialized.
    /// </summary>
    private struct FieldEntry
    {
        /// <summary>
        /// The formatter object to use.
        /// </summary>
        public required object Formatter;

        /// <summary>
        /// The field to serialize.
        /// </summary>
        public required FieldInfo Member;
    }
}
