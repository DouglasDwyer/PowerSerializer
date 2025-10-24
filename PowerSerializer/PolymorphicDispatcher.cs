using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// A type-erased holder for an <see cref="IFormatter{T}"/>.
/// This is used to upcast/downcast with serialization of polymorphic types.
/// </summary>
internal abstract class PolymorphicDispatcher
{
    /// <summary>
    /// Deserializes the contents of <paramref name="reader"/> into a new object,
    /// recording the object reference in the reader's context.
    /// </summary>
    /// <param name="reader">
    /// The input buffer.
    /// </param>
    /// <returns>
    /// The object that was deserialized.
    /// </returns>
    public abstract object RegisterObjectAndDeserialize(BufferReader reader);

    /// <summary>
    /// Serializes the contents of <paramref name="value"/> to the output buffer.
    /// No casting or reference recording is performed.
    /// </summary>
    /// <param name="writer">The output buffer.</param>
    /// <param name="value">The value to record.</param>
    public abstract void SerializeValue(BufferWriter writer, object value);

    /// <summary>
    /// Creates a dispatcher for serializing instances of <paramref name="type"/> using <paramref name="valueFormatter"/>.
    /// </summary>
    /// <param name="type">The concrete type being serialized.</param>
    /// <param name="valueFormatter">
    /// A formatter that determines how to serialize the value contents of <paramref name="type"/>.
    /// </param>
    /// <returns>
    /// A dispatcher that can be used to serialize <paramref name="type"/> in polymorphic scenarios.
    /// </returns>
    public static PolymorphicDispatcher Create(Type type, object valueFormatter)
    {
        return (PolymorphicDispatcher)Activator.CreateInstance(HandlerTypeFor(type), valueFormatter)!;
    }

    /// <summary>
    /// Gets the <see cref="PolymorphicDispatcher"/> implementation to use with a concrete <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The concrete type being serialized.</param>
    /// <returns>The constructed generic implementation of <see cref="PolymorphicDispatcher"/>.</returns>
    private static Type HandlerTypeFor(Type type)
    {
        if (type.IsValueType)
        {
            if (type.GetCustomAttribute<IsReadOnlyAttribute>() is null)
            {
                return typeof(MutableStructDispatcher<>).MakeGenericType(type);
            }
            else
            {
                return typeof(ReadonlyStructDispatcher<>).MakeGenericType(type);
            }
        }
        else
        {
            return typeof(ClassDispatcher<>).MakeGenericType(type);
        }
    }

    /// <summary>
    /// Serializes <c>class</c> types.
    /// </summary>
    /// <typeparam name="T">The concrete type being serialized.</typeparam>
    private sealed class ClassDispatcher<T> : PolymorphicDispatcher where T : class
    {
        /// <summary>
        /// The formatter to use when encoding object contents.
        /// </summary>
        private readonly IFormatter<T> _valueFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="valueFormatter">
        /// The formatter to use when encoding object contents.
        /// </param>
        public ClassDispatcher(IFormatter<T> valueFormatter)
        {
            _valueFormatter = valueFormatter;
        }

        /// <inheritdoc/>
        public override object RegisterObjectAndDeserialize(BufferReader reader)
        {
            ref var result = ref reader.Context.AddObject();

            // Safety: result starts off as null and is only read/written by the deserializer,
            // so this cast does not expose type variance.
            ref var derivedResult = ref Unsafe.As<object?, T?>(ref result);
            _valueFormatter.Deserialize(reader, out derivedResult);

            if (result is null)
            {
                throw new InvalidDataException("Expected non-null object, but deserializer did not initialize output value");
            }

            return result;
        }

        /// <inheritdoc/>
        public override void SerializeValue(BufferWriter writer, object value)
        {
            _valueFormatter.Serialize(writer, (T)value);
        }
    }

    /// <summary>
    /// Serializes boxed <c>struct</c> types.
    /// </summary>
    /// <typeparam name="T">The concrete type being serialized.</typeparam>
    private sealed class MutableStructDispatcher<T> : PolymorphicDispatcher where T : struct
    {
        /// <summary>
        /// The formatter to use when encoding object contents.
        /// </summary>
        private readonly IFormatter<T> _valueFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="valueFormatter">
        /// The formatter to use when encoding object contents.
        /// </param>
        public MutableStructDispatcher(IFormatter<T> valueFormatter)
        {
            if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is not null)
            {
                throw new ArgumentException("readonly structs cannot be serialized by MutableStructHandler", nameof(T));
            }

            _valueFormatter = valueFormatter;
        }
        
        /// <inheritdoc/>
        public override object RegisterObjectAndDeserialize(BufferReader reader)
        {
            object result = default(T)!;
            reader.Context.AddObject() = result;
            _valueFormatter.Deserialize(reader, out Unsafe.Unbox<T>(result));
            return result;
        }

        /// <inheritdoc/>
        public override void SerializeValue(BufferWriter writer, object value)
        {
            _valueFormatter.Serialize(writer, (T)value);
        }
    }

    /// <summary>
    /// Serializes boxed <c>readonly struct</c> types.
    /// </summary>
    /// <typeparam name="T">The concrete type being serialized.</typeparam>
    private sealed class ReadonlyStructDispatcher<T> : PolymorphicDispatcher where T : struct
    {
        /// <summary>
        /// A temporary object to store during deserialization so that the <see cref="DeserializationContext"/>
        /// thinks the boxed object is initialized.
        /// </summary>
        private readonly object _proxy;

        /// <summary>
        /// The formatter to use when encoding object contents.
        /// </summary>
        private readonly IFormatter<T> _valueFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="valueFormatter">
        /// The formatter to use when encoding object contents.
        /// </param>
        public ReadonlyStructDispatcher(IFormatter<T> valueFormatter)
        {
            if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is null)
            {
                throw new ArgumentException("Mutable structs cannot be serialized by MutableStructHandler", nameof(T));
            }

            _proxy = new object();
            _valueFormatter = valueFormatter;
        }

        /// <inheritdoc/>
        public override object RegisterObjectAndDeserialize(BufferReader reader)
        {
            // Note: it is impossible for readonly structs to contain a cyclic reference.
            // Therefore, it is safe to call deserialize before allocating the boxed object,
            // as long as we provide a proxy in the meantime.
            ref var result = ref reader.Context.AddObject();
            result = _proxy;
            _valueFormatter.Deserialize(reader, out var value);
            result = value;
            return result;
        }

        /// <inheritdoc/>
        public override void SerializeValue(BufferWriter writer, object value)
        {
            _valueFormatter.Serialize(writer, (T)value);
        }
    }
}
