using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer;

// todo: better name, this one conflicts with namespace

public sealed class PowerSerializer
{
    /// <summary>
    /// Although these types are not sealed, their derived variants (like <c>RuntimeType</c>)
    /// are almost always internal to C#. As such, they are treated as sealed.
    /// </summary>
    internal static readonly ImmutableHashSet<Type> ArtificallySealedTypes = [
        typeof(Assembly),
        typeof(ConstructorInfo),
        typeof(FieldInfo),
        typeof(MethodInfo),
        typeof(Type),
    ];

    // todo: prevent mutation :(
    /// <summary>
    /// The options that this serializer will use.
    /// </summary>
    public readonly PowerSerializerOptions Options;

    /// <summary>
    /// Formatters that are used to serialize the actual contents of objects.
    /// </summary>
    private readonly ConditionalWeakTable<Type, ContentFormatters> _contentFormatters;

    /// <summary>
    /// Formatters that are used to serialize reference types and boxed value types.
    /// </summary>
    private readonly ConditionalWeakTable<Type, IFormatter> _referenceFormatters;

    public PowerSerializer() : this(new PowerSerializerOptions()) { }

    public PowerSerializer(PowerSerializerOptions options)
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new PlatformNotSupportedException("PowerSerializer requires runtime support for dynamic code generation");
        }

        _contentFormatters = new ConditionalWeakTable<Type, ContentFormatters>();
        _referenceFormatters = new ConditionalWeakTable<Type, IFormatter>();
        Options = options;
    }

    /// <inheritdoc cref="Serialize{T}(IBufferWriter{byte}, in T)"/>
    public ArraySegment<byte> Serialize<T>(in T value)
    {
        var writer = new ArrayBufferWriter<byte>();
        Serialize(writer, value);
        MemoryMarshal.TryGetArray(writer.WrittenMemory, out var result);
        return result;
    }

    /// <summary>
    /// Converts the provided value to binary data.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="writer">Where the output data should be written.</param>
    /// <param name="value">The value to convert.</param>
    /// <returns>
    /// An array segment containing the serialized data.
    /// </returns>
    public void Serialize<T>(IBufferWriter<byte> writer, in T? value)
    {
        var context = SerializationContext.Pool.Get();
        try
        {
            var state = new BufferWriter.State(context, writer);
            GetFormatter<T>().Serialize(new BufferWriter(ref state), value);
            state.Writer.Advance(state.CurrentBlockWritten);
        }
        finally
        {
            SerializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer must contain <b>exactly</b>
    /// the data for <typeparamref name="T"/>, and nothing else. To deserialize only a portion of a buffer,
    /// use the <see cref="Deserialize{T}(ref ReadOnlySpan{byte})"/> overload.
    /// </param>
    /// <returns>The generated object.</returns>
    /// <exception cref="InvalidDataException">
    /// If the input did not describe a valid object.
    /// </exception>
    /// <exception cref="InvalidDataException">
    /// If there was leftover data in the buffer after serialization.
    /// </exception>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to deserialize the type.
    /// </exception>
    public T? Deserialize<T>(ReadOnlySpan<byte> data)
    {
        var result = Deserialize<T>(ref data);
        
        if (0 < data.Length)
        {
            throw new InvalidDataException("Deserialization did not consume all bytes in the provided data");
        }

        return result;
    }

    /// <summary>
    /// Reconstructs a value from a byte array.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the value that was used during serialization.
    /// This must match exactly.
    /// </typeparam>
    /// <param name="data">
    /// A buffer containing the data produced during serialization. This buffer may be <b>larger</b>
    /// than the data for <typeparamref name="T"/>. It will be replaced with a span
    /// containing whatever data remained after deserialization.
    /// </param>
    /// <returns>The generated object.</returns>
    /// <exception cref="InvalidDataException">
    /// If the input did not describe a valid object.
    /// </exception>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize the type.
    /// </exception>
    public T? Deserialize<T>(ref ReadOnlySpan<byte> data)
    {
        var context = DeserializationContext.Pool.Get();
        try
        {
            var position = 0;
            GetFormatter<T>().Deserialize(new BufferReader(context, data, ref position), out var result);
            data = data[position..];
            return result;
        }
        finally
        {
            DeserializationContext.Pool.Return(context);
        }
    }

    /// <summary>
    /// Gets the formatter to use when serializing objects with base type <paramref name="type"/>.
    /// </summary>
    /// <param name="type">
    /// The base class of all objects to be serialized.
    /// </param>
    /// <returns>
    /// The formatter to use. This can be cast to <see cref="IFormatter{T}"/> where <c>T</c> equals <paramref name="type"/>.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    public IFormatter GetFormatter(Type type)
    {
        if (type.IsValueType)
        {
            return _contentFormatters.GetValue(type, CreateContentFormatters).ContentFormatter;
        }
        else
        {
            return _referenceFormatters.GetValue(type, CreateReferenceFormatter);
        }
    }

    /// <summary>
    /// Gets the formatter to use when serializing objects with base type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The base class of all objects to be serialized.
    /// </typeparam>
    /// <returns>
    /// The formatter to use.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    public IFormatter<T?> GetFormatter<T>()
    {
        return (IFormatter<T?>)GetFormatter(typeof(T));
    }

    /// <summary>
    /// Gets a type-erased dispatcher for serializing/deserializing
    /// objects whose true type is <paramref name="type"/>.
    /// </summary>
    /// <param name="type">The actual, concrete object type.</param>
    /// <returns>
    /// A dispatcher that can be used to serialize and deserialize it.
    /// </returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    internal IFormatter<object> GetPolymorphicDispatcher(Type type)
    {
        return _contentFormatters.GetValue(type, CreateContentFormatters).PolymorphicDispatcher;
    }

    /// <summary>
    /// Generates new content formatters for the given type.
    /// </summary>
    /// <param name="type">
    /// The concrete type being serialized.
    /// </param>
    /// <returns></returns>
    /// <exception cref="MissingFormatterException">
    /// If no formatter could be found to serialize/deserialize the type.
    /// </exception>
    private ContentFormatters CreateContentFormatters(Type type)
    {
        foreach (var sealedType in ArtificallySealedTypes)
        {
            if (type.IsAssignableTo(sealedType) && type != sealedType)
            {
                return CreateContentFormatters(sealedType);
            }
        }

        IFormatter? contentFormatter = null;
        foreach (var resolver in Options.Resolvers)
        {
            if (resolver.GetFormatter(this, type) is IFormatter newFormatter)
            {
                contentFormatter = newFormatter;
                break;
            }
        }

        if (contentFormatter is null)
        {
            throw new MissingFormatterException(type);
        }

        var polymorphicDispatcher = PolymorphicDispatcher.Create(type, contentFormatter);
        return new ContentFormatters { ContentFormatter = contentFormatter, PolymorphicDispatcher = polymorphicDispatcher };
    }

    /// <summary>
    /// Creates the reference formatter for the provided type.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns>
    /// A formatter for serializing <paramref name="type"/> that respects object references.
    /// </returns>
    private IFormatter CreateReferenceFormatter(Type type)
    {
        return (IFormatter)Activator.CreateInstance(typeof(ReferenceFormatter<>).MakeGenericType(type), [this])!;
    }

    /// <summary>
    /// Holds formatters that are used to serialize the actual contents of an object.
    /// </summary>
    private class ContentFormatters
    {
        /// <summary>
        /// The "value" formatter that defines how to serialize the actual contents of this type.
        /// This formatter does not deal with references or polymorphism.
        /// </summary>
        public required IFormatter ContentFormatter;

        /// <summary>
        /// A shim for invoking the <see cref="ContentFormatter"/> in a weakly-typed context.
        /// This is used when serializing polymorphic reference types and boxed value types.
        /// </summary>
        public required IFormatter<object> PolymorphicDispatcher;
    }
}