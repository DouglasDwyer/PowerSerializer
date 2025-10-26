using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Implements the logic for serializing and deserializing reference types - namely,
/// the conversion from objects to IDs.
/// </summary>
/// <typeparam name="T">The reference type being serialized.</typeparam>
internal sealed class ReferenceFormatter<T> : IFormatter<T?> where T : class
{
    /// <summary>
    /// Whether the reference type is sealed (that is, cannot be inherited from).
    /// If this is the case, then the <see cref="_contentFormatter"/> can be cached
    /// after first use to eliminate the overhead of dynamic lookups.
    /// </summary>
    private static readonly bool IsSealed = IsTypeSealed(typeof(T));

    /// <summary>
    /// If this is a sealed formatter, then the subformatter to use for recording actual object values.
    /// </summary>
    private IFormatter<object>? _contentFormatter;

    /// <summary>
    /// The associated serializer instance.
    /// </summary>
    private readonly PowerSerializer _serializer;

    /// <summary>
    /// If this is a polymorphic formatter, then the subformatter to use for recording object types.
    /// </summary>
    private IFormatter<Type?>? _typeFormatter;

    /// <summary>
    /// Instantiates a new reference formatter.
    /// </summary>
    /// <param name="serializer">The associated serializer instance.</param>
    public ReferenceFormatter(PowerSerializer serializer)
    {
        _serializer = serializer;
        // Note: the type formatter must be lazily initialized or we would have
        // an infinite loop constructing ReferenceFormatter<Type> instances!
        _typeFormatter = null;
        _contentFormatter = null;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T? value)
    {
        var reference = ReferenceId.Read(reader);
        if (reference == ReferenceId.Null)
        {
            value = null;
        }
        else if (reference == ReferenceId.New)
        {
            ref var slot = ref reader.Context.AllocateReference();

            LazyInitializeFormatters();
            if (IsSealed)
            {
                _contentFormatter!.Deserialize(reader, out slot);
            }
            else
            {
                _typeFormatter!.Deserialize(reader, out var type);
                
                if (type is null)
                {
                    throw new InvalidDataException("Polymorphic type was not encoded properly: expected type, but got null");
                }

                _serializer.GetPolymorphicDispatcher(type).Deserialize(reader, out slot);
            }

            value = (T)slot;
        }
        else
        {
            value = (T)reader.Context.GetExistingReference(reference.Index);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        if (value is null)
        {
            ReferenceId.Null.Write(writer);
        }
        else if (writer.Context.GetExistingReference(value) is uint id)
        {
            ReferenceId.Existing(id).Write(writer);
        }
        else
        {
            ReferenceId.New.Write(writer);
            writer.Context.AllocateReference(value);

            LazyInitializeFormatters();
            if (IsSealed)
            {
                _contentFormatter!.Serialize(writer, value);
            }
            else
            {
                var type = value.GetType();
                _typeFormatter!.Serialize(writer, type);
                _serializer.GetPolymorphicDispatcher(type).Serialize(writer, value);
            }
        }
    }

    /// <summary>
    /// Initializes the <see cref="_typeFormatter"/> and <see cref="_contentFormatter"/>
    /// members by fetching the appropriate formatters from the serializer instance.
    /// </summary>
    private void LazyInitializeFormatters()
    {
        if (IsSealed)
        {
            if (_contentFormatter is null)
            {
                _contentFormatter = _serializer.GetPolymorphicDispatcher(typeof(T));
            }
        }
        else
        {
            if (_typeFormatter is null)
            {
                _typeFormatter = _serializer.GetFormatter<Type>();
            }
        }
    }

    /// <summary>
    /// Determines whether the given type cannot have derived types.
    /// </summary>
    /// <param name="type">
    /// The type to check.
    /// </param>
    /// <returns>
    /// <c>true</c> if the class should <b>not</b> be treated as polymorphic.
    /// </returns>
    private static bool IsTypeSealed(Type type)
    {
        if (type.IsArray)
        {
            return IsTypeSealed(type.GetElementType()!);
        }
        else
        {
            return type.IsSealed
                || PowerSerializer.ArtificallySealedTypes.Any(type.IsAssignableTo);
        }
    }

    /// <summary>
    /// Encodes an object reference.
    /// </summary>
    private readonly record struct ReferenceId
    {
        /// <summary>
        /// The reference corresponds to a new object.
        /// </summary>
        public static readonly ReferenceId New = new ReferenceId(1);

        /// <summary>
        /// The reference is null, and does not correspond to any object.
        /// </summary>
        public static readonly ReferenceId Null = new ReferenceId(0);

        /// <summary>
        /// Gets the index associated with this ID.
        /// </summary>
        public uint Index
        {
            get
            {
                if (2 <= _inner)
                {
                    return _inner - 2;
                }
                else
                {
                    throw new InvalidOperationException("Reference did not correspond to an index");
                }
            }
        }

        /// <summary>
        /// The inner representation of the reference.
        /// </summary>
        private readonly uint _inner;

        /// <summary>
        /// Creates a new reference ID.
        /// </summary>
        /// <param name="inner">The inner representation of the ID.</param>
        private ReferenceId(uint inner)
        {
            _inner = inner;
        }

        /// <summary>
        /// The reference corresponds to an existing object that has been seen before.
        /// </summary>
        /// <param name="id">The ID of the existing object.</param>
        /// <returns>An encoded reference for that ID.</returns>
        public static ReferenceId Existing(uint id)
        {
            return new ReferenceId(id + 2);
        }

        /// <summary>
        /// Decodes the reference ID from the input.
        /// </summary>
        /// <param name="reader">The input buffer.</param>
        /// <returns>The decoded reference ID.</returns>
        public static ReferenceId Read(BufferReader reader)
        {
            return new ReferenceId(reader.ReadVarUInt32());
        }

        /// <summary>
        /// Encodes the reference ID and writes it to the output.
        /// </summary>
        /// <param name="writer">The output buffer.</param>
        public void Write(BufferWriter writer)
        {
            writer.WriteVarUInt32(_inner);
        }
    }
}
