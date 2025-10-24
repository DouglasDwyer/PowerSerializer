using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes an array element-by-element.
/// </summary>
/// <typeparam name="A">The array type itself.</typeparam>
public sealed class ArrayFormatter<A> : IFormatter<A> where A : notnull
{
    /// <summary>
    /// The specialized formatter type to use.
    /// </summary>
    private readonly IFormatter<A> _concreteFormatter;

    /// <summary>
    /// Creates a new array formatter.
    /// </summary>
    /// <param name="serializer">The serializer that will use this format.</param>
    /// <exception cref="ArgumentException">
    /// If <typeparamref name="A"/> was not a valid array type.
    /// </exception>
    public ArrayFormatter(PowerSerializer serializer)
    {
        _concreteFormatter = ArrayFormatterHelpers.GetConcreteFormatter<A>(serializer, typeof(ConcreteFormatter<>));
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        _concreteFormatter.Deserialize(reader, out value);
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        _concreteFormatter.Serialize(writer, value);
    }

    /// <summary>
    /// Specialized formatter implementation.
    /// </summary>
    /// <typeparam name="T">The element type of the array.</typeparam>
    private sealed class ConcreteFormatter<T> : ArrayFormatterHelpers.ConcreteFormatterBase<T, A>
    {
        /// <summary>
        /// The formatter to use for individual array elements.
        /// </summary>
        private readonly IFormatter<T?> _elementFormatter;

        public ConcreteFormatter(PowerSerializer serializer)
        {
            _elementFormatter = serializer.GetFormatter<T>();
        }

        /// <inheritdoc/>
        protected override void DeserializeElements(BufferReader reader, Span<T?> elements)
        {
            for (var i = 0; i < elements.Length; i++)
            {
                _elementFormatter.Deserialize(reader, out elements[i]);
            }
        }

        /// <inheritdoc/>
        protected override void SerializeElements(BufferWriter writer, Span<T?> elements)
        {
            for (var i = 0; i < elements.Length; i++)
            {
                _elementFormatter.Serialize(writer, elements[i]);
            }
        }
    }
}