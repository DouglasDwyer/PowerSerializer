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
internal sealed class SealedReferenceFormatter<T> : IFormatter<T?> where T : class
{
    /// <summary>
    /// The formatter to use when serializing the actual object contents.
    /// </summary>
    private readonly IFormatter<T> _valueFormatter;

    /// <summary>
    /// Creates a new formatter.
    /// </summary>
    /// <param name="valueFormatter">
    /// The formatter to use when serializing the actual object contents.
    /// </param>
    public SealedReferenceFormatter(IFormatter<T> valueFormatter)
    {
        _valueFormatter = valueFormatter;
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
            ref var result = ref reader.Context.AddObject();

            // Safety: result starts off as null and is only read/written by the deserializer,
            // so this cast does not expose type variance.
            ref var derivedResult = ref Unsafe.As<object?, T?>(ref result);
            _valueFormatter.Deserialize(reader, out derivedResult);

            if (result is null)
            {
                throw new InvalidDataException("Expected non-null object, but deserializer did not initialize output value");
            }

            value = derivedResult;
        }
        else
        {
            value = (T)reader.Context.GetExistingObject(reference.Index);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        var reference = writer.Context.AddOrGetReference(value);
        reference.Write(writer);
        if (reference == ReferenceId.New)
        {
            _valueFormatter.Serialize(writer, value!);
        }
    }
}
