using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes <see cref="Type"/> objects, including arrays and generics.
/// </summary>
public sealed class TypeFormatter : IFormatter<Type>
{
    /// <summary>
    /// Formats assembly references.
    /// </summary>
    private readonly IFormatter<Assembly?> _assemblyFormatter;

    /// <summary>
    /// Formats method references. Used when serializing the generic
    /// type parameters of methods.
    /// </summary>
    private readonly IFormatter<MethodInfo?> _methodFormatter;

    /// <summary>
    /// A reference serializer that will recursively fall back to this
    /// <see cref="TypeFormatter"/> when it encounters a new type.
    /// </summary>
    private readonly IFormatter<Type?> _typeReferenceFormatter;

    /// <summary>
    /// Initializes a type formatter.
    /// </summary>
    /// <param name="serializer">
    /// The serializer associated with this formatter.
    /// </param>
    public TypeFormatter(PowerSerializer serializer)
    {
        _assemblyFormatter = serializer.GetFormatter<Assembly>();
        _methodFormatter = null!;// serializer.GetFormatter<MethodInfo>();
        _typeReferenceFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Type value)
    {
        // todo: handle array

        var kind = (GenericKind)reader.ReadUInt8();

        if (kind == GenericKind.TypeParameter)
        {
            var position = reader.ReadUInt8();
            _typeReferenceFormatter.Deserialize(reader, out var parent);
            value = parent!.GetGenericArguments()[position];
        }
        else if (kind == GenericKind.MethodParameter)
        {
            var position = reader.ReadUInt8();
            _methodFormatter.Deserialize(reader, out var parent);
            value = parent!.GetGenericArguments()[position];
        }
        else if (kind == GenericKind.Constructed)
        {
            _typeReferenceFormatter.Deserialize(reader, out var definition);
            ThrowInvalidDataExceptionIfNull(definition, "Generic type was not encoded properly: expected type definition, but got null");

            var typeCount = definition!.GetGenericArguments().Length;  // todo: cache
            var types = new Type[typeCount];

            for (var i = 0; i < typeCount; i++)
            {
                _typeReferenceFormatter.Deserialize(reader, out var argument);
                ThrowInvalidDataExceptionIfNull(argument, "Generic type was not encoded properly: expected type argument, but got null");
                types[i] = argument;
            }

            value = definition.MakeGenericType(types);
        }
        else
        {
            var genericCount = kind.Count;
            var rawName = reader.ReadString(Encoding.ASCII);
            var fullName = 0 < genericCount ? $"{rawName}`{genericCount}" : rawName;

            _assemblyFormatter.Deserialize(reader, out var assembly);
            ThrowInvalidDataExceptionIfNull(assembly, "Type was not encoded properly: expected assembly, but got null");
            var result = assembly.GetType(fullName);

            if (result is null)
            {
                throw new TypeLoadException($"Unable to load type {fullName} from {assembly.FullName}");
            }

            value = result;
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Type value)
    {
        if (value.IsGenericTypeParameter)
        {
            writer.WriteUInt8((byte)GenericKind.TypeParameter);
            writer.WriteUInt8((byte)value.GenericParameterPosition);
            _typeReferenceFormatter.Serialize(writer, value.DeclaringType);
        }
        else if (value.IsGenericMethodParameter)
        {
            writer.WriteUInt8((byte)GenericKind.MethodParameter);
            writer.WriteUInt8((byte)value.GenericParameterPosition);
            _methodFormatter.Serialize(writer, (MethodInfo)value.DeclaringMethod!);
        }
        else if (value.IsConstructedGenericType)
        {
            writer.WriteUInt8((byte)GenericKind.Constructed);
            _typeReferenceFormatter.Serialize(writer, value.GetGenericTypeDefinition());

            foreach (var ty in value.GetGenericArguments())   // todo: cache
            {
                _typeReferenceFormatter.Serialize(writer, ty);
            }
        }
        else if (!value.ContainsGenericParameters || value.IsGenericTypeDefinition)
        {
            var genericCount = 0;
            var genericDefinition = value;

            if (value.IsGenericTypeDefinition)
            {
                genericDefinition = value.GetGenericTypeDefinition();
                genericCount = genericDefinition.GetGenericArguments().Length;  // todo: cache
            }

            writer.WriteUInt8((byte)GenericKind.Definition(genericCount));
            writer.WriteString(NamespaceQualifiedName(genericDefinition), Encoding.ASCII);
            _assemblyFormatter.Serialize(writer, genericDefinition.Assembly);
        }
        else
        {
            throw new InvalidOperationException("Unrecognized kind of type");
        }
    }

    private void ThrowInvalidDataExceptionIfNull<T>([NotNull] T? value, string message) where T : class
    {
        if (value is null)
        {
            throw new InvalidDataException(message);
        }
    }

    private static string NamespaceQualifiedName(Type type)
    {
        var result = type.Name;
        var backTickIndex = result.IndexOf("`");
        if (0 <= backTickIndex)
        {
            return result[..backTickIndex];
        }
        else
        {
            return result;
        }
    }

    /// <summary>
    /// Describes what "kind" of generic a type is - whether it is a simple type,
    /// a generic parameter (such as <c>T</c>), or a generic type with parameters.
    /// </summary>
    private readonly record struct GenericKind
    {
        /// <summary>
        /// The type is a constructed generic with type parameters.
        /// </summary>
        public static readonly GenericKind Constructed = new GenericKind(253);

        /// <summary>
        /// The type is a stand-in generic parameter (such as <c>T</c>) declared on a method.
        /// </summary>
        public static readonly GenericKind MethodParameter = new GenericKind(254);

        /// <summary>
        /// The type is a stand-in generic parameter (such as <c>T</c>) declared on a type.
        /// </summary>
        public static readonly GenericKind TypeParameter = new GenericKind(255);

        /// <summary>
        /// If this represents a generic type <b>definition</b>, gets the number of parameters.
        /// Otherwise, throws an exception.
        /// </summary>
        public int Count
        {
            get
            {
                if (_inner < 253)
                {
                    return _inner;
                }
                else
                {
                    throw new InvalidOperationException("Generic kind did not have an associated count");
                }
            }
        }

        /// <summary>
        /// The inner representation of the generic kind.
        /// </summary>
        private readonly byte _inner;

        /// <summary>
        /// Creates a new kind with the provided representation.
        /// </summary>
        /// <param name="inner">The inner representation of this type.</param>
        private GenericKind(byte inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Gets a kind representing a generic type definition (or simple type) with <paramref name="count"/> generic parameters.
        /// </summary>
        /// <param name="count">The generic arity of the type. This may be <c>0</c>.</param>
        /// <returns>The encoded kind.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the number of generic parameters exceeds the maximum representible value.
        /// </exception>
        public static GenericKind Definition(int count)
        {
            if (count < 253)
            {
                return new GenericKind((byte)count);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Exceeded maximum supported number of generic parameters");
            }
        }

        /// <summary>
        /// Converts a kind to its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator byte(GenericKind value) => value._inner;

        /// <summary>
        /// Gets a kind from its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator GenericKind(byte value) => new GenericKind(value);
    }
}
