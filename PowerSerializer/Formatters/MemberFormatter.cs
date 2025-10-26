using FastExpressionCompiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Formatters;

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
            bodyStatements[i + 1] = Expression.Call(
                Expression.Constant(entry.Formatter),
                GetDeserializeImplementation(entry.Formatter, entry.Member.FieldType),
                readerParam,
                Expression.Field(valueParam, entry.Member));
        }

        var body = Expression.Block(bodyStatements);
        var lambda = Expression.Lambda<DeserializeDelegate>(body, readerParam, valueParam);

        return lambda.CompileFast(false, CompilerFlags.DisableInterpreter | CompilerFlags.ThrowOnNotSupportedExpression);
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
                GetSerializeImplementation(entry.Formatter, entry.Member.FieldType),
                writerParam,
                Expression.Field(valueParam, entry.Member));
        }

        var body = Expression.Block(bodyStatements);
        var lambda = Expression.Lambda<SerializeDelegate>(body, writerParam, valueParam);

        return lambda.CompileFast(false, CompilerFlags.DisableInterpreter | CompilerFlags.ThrowOnNotSupportedExpression);
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

    /// <summary>
    /// Gets the concrete deserialize method for a formatter.
    /// </summary>
    /// <param name="formatter">The formatter object.</param>
    /// <param name="targetType">The type to be deserialized.</param>
    /// <returns>
    /// The implementation of <see cref="IFormatter{T}.Deserialize(BufferReader, out T)"/> that will be invoked.
    /// </returns>
    private static MethodInfo GetDeserializeImplementation(object formatter, Type targetType)
    {
        return GetImplementationMethod(
            formatter.GetType(),
            typeof(IFormatter<>).MakeGenericType(targetType).GetMethod(nameof(IFormatter<bool>.Deserialize))!);
    }

    /// <summary>
    /// Gets the concrete serialize method for a formatter.
    /// </summary>
    /// <param name="formatter">The formatter object.</param>
    /// <param name="targetType">The type to be serialized.</param>
    /// <returns>
    /// The implementation of <see cref="IFormatter{T}.Serialize(BufferWriter, in T)"/> that will be invoked.
    /// </returns>
    private static MethodInfo GetSerializeImplementation(object formatter, Type targetType)
    {
        return GetImplementationMethod(
            formatter.GetType(),
            typeof(IFormatter<>).MakeGenericType(targetType).GetMethod(nameof(IFormatter<bool>.Serialize))!);
    }

    /// <summary>
    /// Gets the concrete implementation of an interface method.
    /// </summary>
    /// <param name="implementingClass">The class implementing the interface.</param>
    /// <param name="interfaceMethod">The interface method definition.</param>
    /// <returns>The corresponding implementation method.</returns>
    /// <exception cref="ArgumentException">
    /// If <paramref name="interfaceMethod"/> was not an interface method.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// If <paramref name="implementingClass"/> did not implement the interface to which <paramref name="interfaceMethod"/> belongs.
    /// </exception>
    private static MethodInfo GetImplementationMethod(Type implementingClass, MethodInfo interfaceMethod)
    {
        if (interfaceMethod.DeclaringType is null || !interfaceMethod.DeclaringType.IsInterface)
        {
            throw new ArgumentException($"Expected method {interfaceMethod} to belong to interface");
        }

        if (!implementingClass.IsAssignableTo(interfaceMethod.DeclaringType))
        {
            throw new ArgumentException($"Expected implementing class {implementingClass} to be assignable to interface {interfaceMethod.DeclaringType}", nameof(implementingClass));
        }

        var map = implementingClass.GetInterfaceMap(interfaceMethod.DeclaringType);
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
        public required IFormatter Formatter;

        /// <summary>
        /// The field to serialize.
        /// </summary>
        public required FieldInfo Member;
    }
}
