using System.IO;
using System.Runtime.CompilerServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes objects of type <typeparamref name="T"/> without polymorphism.
/// If an object is seen multiple times, then a short reference ID is encoded
/// rather than copying the object twice.
/// </summary>
/// <typeparam name="T">
/// The concrete type to serialize.
/// </typeparam>
internal sealed class SealedReferenceFormatter<T> : ReferenceFormatterBase<T>, ISealedReferenceFormatter where T : class
{
    /// <summary>
    /// The formatter to use when serializing the actual object contents.
    /// </summary>
    private IFormatter<T>? _valueFormatter;

    /// <inheritdoc/>
    public void SetValueFormatter(object formatter)
    {
        _valueFormatter = (IFormatter<T>)formatter;
    }

    /// <inheritdoc/>
    protected override T RegisterObjectAndDeserialize(BufferReader reader)
    {
        ref var result = ref reader.Context.AllocateReference();

        // Safety: result starts off as null and is only read/written by the deserializer,
        // so this cast does not expose type variance.
        ref var derivedResult = ref Unsafe.As<object?, T?>(ref result);
        _valueFormatter!.Deserialize(reader, out derivedResult);

        if (result is null)
        {
            throw new InvalidDataException("Expected non-null object, but deserializer did not initialize output value");
        }

        return derivedResult;
    }

    /// <inheritdoc/>
    protected override void SerializeValue(BufferWriter writer, T value)
    {
        _valueFormatter!.Serialize(writer, value);
    }
}