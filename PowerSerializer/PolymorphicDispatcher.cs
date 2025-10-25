using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// A type-erased holder for an <see cref="IFormatter{T}"/>.
/// This is used to upcast/downcast with serialization of polymorphic types.
/// </summary>
internal static class PolymorphicDispatcher
{
    /// <summary>
    /// Creates a dispatcher for serializing instances of <paramref name="type"/> using <paramref name="contentFormatter"/>.
    /// </summary>
    /// <param name="type">The concrete type being serialized.</param>
    /// <param name="contentFormatter">
    /// A formatter that determines how to serialize the value contents of <paramref name="type"/>.
    /// </param>
    /// <returns>
    /// A dispatcher that can be used to serialize <paramref name="type"/> in polymorphic scenarios.
    /// </returns>
    public static IFormatter<object> Create(Type type, object contentFormatter)
    {
        return (IFormatter<object>)Activator.CreateInstance(HandlerTypeFor(type), contentFormatter)!;
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
    private sealed class ClassDispatcher<T> : IFormatter<object> where T : class
    {
        /// <summary>
        /// The formatter to use when encoding actual object values.
        /// </summary>
        private readonly IFormatter<T> _contentFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="contentFormatter">
        /// The formatter to use when encoding actual object values.
        /// </param>
        public ClassDispatcher(IFormatter<T> contentFormatter)
        {
            _contentFormatter = contentFormatter;
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, out object value)
        {
            value = null!;

            // Safety: result starts off as null and is only read/written by the deserializer,
            // so this cast does not expose type variance.
            ref var derivedResult = ref Unsafe.As<object, T?>(ref value);
            _contentFormatter.Deserialize(reader, out derivedResult);

            if (derivedResult is null)
            {
                throw new InvalidDataException("Expected non-null object, but deserializer did not initialize output value");
            }
        }

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, in object value)
        {
            _contentFormatter.Serialize(writer, (T)value);
        }
    }

    /// <summary>
    /// Serializes boxed <c>struct</c> types.
    /// </summary>
    /// <typeparam name="T">The concrete type being serialized.</typeparam>
    private sealed class MutableStructDispatcher<T> : IFormatter<object> where T : struct
    {
        /// <summary>
        /// The formatter to use when encoding actual object values.
        /// </summary>
        private readonly IFormatter<T> _contentFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="contentFormatter">
        /// The formatter to use when encoding actual object values.
        /// </param>
        public MutableStructDispatcher(IFormatter<T> contentFormatter)
        {
            if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is not null)
            {
                throw new ArgumentException("readonly structs cannot be serialized by MutableStructHandler", nameof(T));
            }

            _contentFormatter = contentFormatter;
        }
        
        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, out object value)
        {
            value = default(T)!;
            _contentFormatter.Deserialize(reader, out Unsafe.Unbox<T>(value));
        }

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, in object value)
        {
            _contentFormatter.Serialize(writer, (T)value);
        }
    }

    /// <summary>
    /// Serializes boxed <c>readonly struct</c> types.
    /// </summary>
    /// <typeparam name="T">The concrete type being serialized.</typeparam>
    private sealed class ReadonlyStructDispatcher<T> : IFormatter<object> where T : struct
    {
        /// <summary>
        /// The formatter to use when encoding actual object values.
        /// </summary>
        private readonly IFormatter<T> _contentFormatter;

        /// <summary>
        /// Creates a new handler object.
        /// </summary>
        /// <param name="contentFormatter">
        /// The formatter to use when encoding actual object values.
        /// </param>
        public ReadonlyStructDispatcher(IFormatter<T> contentFormatter)
        {
            if (typeof(T).GetCustomAttribute<IsReadOnlyAttribute>() is null)
            {
                throw new ArgumentException("Mutable structs cannot be serialized by MutableStructHandler", nameof(T));
            }

            _contentFormatter = contentFormatter;
        }

        /// <inheritdoc/>
        public void Deserialize(BufferReader reader, out object value)
        {
            // Note: it is impossible for readonly structs to contain a cyclic reference.
            // Therefore, it is safe to call deserialize before allocating the boxed object.
            _contentFormatter.Deserialize(reader, out var result);
            value = result;
        }

        /// <inheritdoc/>
        public void Serialize(BufferWriter writer, in object value)
        {
            _contentFormatter.Serialize(writer, (T)value);
        }
    }
}
