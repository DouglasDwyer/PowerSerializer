using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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

    private readonly NameMap<Type> _knownTypes;

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
        _knownTypes = new NameMap<Type>(serializer.Options.KnownAssemblies.Where(x => !x.IsDynamic).SelectMany(x => x.GetTypes()),
            t => $"[{t.Assembly.GetName().Name}]{t.FullName!}");
        _methodFormatter = null!;// serializer.GetFormatter<MethodInfo>();
        _typeReferenceFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out Type value)
    {
        var metadata = (TypeMetadata)reader.ReadUInt8();
        switch (metadata.Kind)
        {
            case TypeKind.SZArray:
            {
                _typeReferenceFormatter.Deserialize(reader, out var element);
                ThrowInvalidDataExceptionIfNull(element, "Array type was not encoded properly: expected element type, but got null");
                value = element.MakeArrayType();
                break;
            }
            case TypeKind.Array:
            {
                _typeReferenceFormatter.Deserialize(reader, out var element);
                ThrowInvalidDataExceptionIfNull(element, "Array type was not encoded properly: expected element type, but got null");
                value = element.MakeArrayType(metadata.Arity);
                break;
            }
            case TypeKind.TypeParameter:
            {
                var position = reader.ReadUInt8();
                _typeReferenceFormatter.Deserialize(reader, out var parent);
                value = parent!.GetGenericArguments()[position];
                break;
            }
            case TypeKind.MethodParameter:
            {
                var position = reader.ReadUInt8();
                _methodFormatter.Deserialize(reader, out var parent);
                value = parent!.GetGenericArguments()[position];
                break;
            }
            case TypeKind.ConstructedGeneric:
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
                break;
                }
            case TypeKind.KnownDefinition:
            {
                var id = reader.ReadUInt64();
                if (!_knownTypes.TryGetObject(id, out value!))
                {
                    throw new TypeLoadException("Could not find well-known type by hash; an assembly may be missing from the PowerSerializerOptions.KnownAssemblies list");
                }
                break;
            }
            case TypeKind.Definition:
            default:
            {
                var rawName = reader.ReadString(Encoding.ASCII);
                var fullName = 0 < metadata.Arity ? $"{rawName}`{metadata.Arity}" : rawName;

                _assemblyFormatter.Deserialize(reader, out var assembly);
                ThrowInvalidDataExceptionIfNull(assembly, "Type was not encoded properly: expected assembly, but got null");
                var result = assembly.GetType(fullName);

                if (result is null)
                {
                    throw new TypeLoadException($"Unable to load type {fullName} from {assembly.FullName}");
                }

                value = result;
                break;
            }
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in Type value)
    {
        if (value.IsSZArray)
        {
            writer.WriteUInt8((byte)TypeMetadata.SZArray());
            _typeReferenceFormatter.Serialize(writer, value.GetElementType());
        }
        else if (value.IsArray)
        {
            writer.WriteUInt8((byte)TypeMetadata.Array(value.GetArrayRank()));
            _typeReferenceFormatter.Serialize(writer, value.GetElementType());
        }
        else if (value.IsGenericTypeParameter)
        {
            writer.WriteUInt8((byte)TypeMetadata.TypeParameter());
            writer.WriteUInt8((byte)value.GenericParameterPosition);
            _typeReferenceFormatter.Serialize(writer, value.DeclaringType);
        }
        else if (value.IsGenericMethodParameter)
        {
            writer.WriteUInt8((byte)TypeMetadata.MethodParameter());
            writer.WriteUInt8((byte)value.GenericParameterPosition);
            _methodFormatter.Serialize(writer, (MethodInfo)value.DeclaringMethod!);
        }
        else if (value.IsConstructedGenericType)
        {
            writer.WriteUInt8((byte)TypeMetadata.ConstructedGeneric());
            _typeReferenceFormatter.Serialize(writer, value.GetGenericTypeDefinition());

            foreach (var ty in value.GetGenericArguments())   // todo: cache
            {
                _typeReferenceFormatter.Serialize(writer, ty);
            }
        }
        else if (!value.ContainsGenericParameters || value.IsGenericTypeDefinition)
        {
            if (_knownTypes.TryGetId(value, out var id))
            {
                writer.WriteUInt8((byte)TypeMetadata.KnownDefinition());
                writer.WriteUInt64(id);
            }
            else
            {
                var genericCount = 0;
                var genericDefinition = value;

                if (value.IsGenericTypeDefinition)
                {
                    genericDefinition = value.GetGenericTypeDefinition();
                    genericCount = genericDefinition.GetGenericArguments().Length;  // todo: cache
                }

                writer.WriteUInt8((byte)TypeMetadata.Definition(genericCount));
                writer.WriteString(NamespaceQualifiedName(genericDefinition), Encoding.ASCII);
                _assemblyFormatter.Serialize(writer, genericDefinition.Assembly);
            }
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
        var result = string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
        var backTickIndex = result.LastIndexOf("`");
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
    /// Identifies a specific subset of types.
    /// </summary>
    private enum TypeKind
    {
        /// <summary>
        /// An array type.
        /// </summary>
        Array,

        /// <summary>
        /// A constructed generic type.
        /// </summary>
        ConstructedGeneric,

        /// <summary>
        /// A non-generic type or an open generic type.
        /// </summary>
        Definition,

        /// <summary>
        /// A non-generic or open generic type from one of the <see cref="PowerSerializerOptions.KnownAssemblies"/>.
        /// </summary>
        KnownDefinition,

        /// <summary>
        /// A generic method parameter.
        /// </summary>
        MethodParameter,

        /// <summary>
        /// A 1D array type with a lower bound of zero.
        /// </summary>
        SZArray,

        /// <summary>
        /// A generic type parameter.
        /// </summary>
        TypeParameter,
    }

    /// <summary>
    /// Records information about a type that is not already encoded in the name.
    /// This includes the number of generic parameters and array dimensions.
    /// </summary>
    private readonly record struct TypeMetadata
    {
        /// <summary>
        /// Describes what sort of type this is.
        /// </summary>
        public TypeKind Kind => (TypeKind)(_inner & 0b111);

        /// <summary>
        /// Gets the arity (if any) associated with the type.
        /// </summary>
        public int Arity
        {
            get
            {
                if (Kind == TypeKind.Definition)
                {
                    return _inner >> 3;
                }
                else if (Kind == TypeKind.Array)
                {
                    return (_inner >> 3) + 1;
                }
                else
                {
                    throw new InvalidOperationException("Type metadata not associated with an arity");
                }
            }
        }

        /// <summary>
        /// The inner representation of the metadata.
        /// </summary>
        private readonly byte _inner;

        /// <summary>
        /// Creates a new metadata object.
        /// </summary>
        /// <param name="inner">The inner representation of the metadata.</param>
        private TypeMetadata(byte inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// Creates a new metadata object.
        /// </summary>
        /// <param name="kind">The subset to which this type belongs.</param>
        /// <param name="number">A numeric value associated with the type, used to encode extra properties.</param>
        private TypeMetadata(TypeKind kind, int number)
        {
            _inner = (byte)((byte)kind | (number << 3));
        }

        /// <summary>
        /// An array type.
        /// </summary>
        /// <param name="dimensions">The number of array dimensions.</param>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata Array(int dimensions)
        {
            return new TypeMetadata(TypeKind.Array, dimensions - 1);
        }

        /// <summary>
        /// A constructed generic type.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata ConstructedGeneric()
        {
            return new TypeMetadata(TypeKind.ConstructedGeneric, 0);
        }

        /// <summary>
        /// A non-generic type or an open generic type.
        /// </summary>
        /// <param name="genericArity">The number of generic arguments.</param>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata Definition(int genericArity)
        {
            return new TypeMetadata(TypeKind.Definition, genericArity);
        }

        /// <summary>
        /// A non-generic or open generic type from one of the <see cref="PowerSerializerOptions.KnownAssemblies"/>.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata KnownDefinition()
        {
            return new TypeMetadata(TypeKind.KnownDefinition, 0);
        }

        /// <summary>
        /// A generic method parameter.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata MethodParameter()
        {
            return new TypeMetadata(TypeKind.MethodParameter, 0);
        }

        /// <summary>
        /// An array type with variable lower bounds.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata SZArray()
        {
            return new TypeMetadata(TypeKind.SZArray, 0);
        }

        /// <summary>
        /// A generic type parameter.
        /// </summary>
        /// <returns>The associated metadata.</returns>
        public static TypeMetadata TypeParameter()
        {
            return new TypeMetadata(TypeKind.TypeParameter, 0);
        }

        /// <summary>
        /// Converts the metadata to its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator byte(TypeMetadata value) => value._inner;

        /// <summary>
        /// Gets metadata from its underlying representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        public static explicit operator TypeMetadata(byte value) => new TypeMetadata(value);
    }
}
