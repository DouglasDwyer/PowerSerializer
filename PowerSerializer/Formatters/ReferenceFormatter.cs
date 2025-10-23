using System;
using System.Reflection.PortableExecutable;

namespace DouglasDwyer.PowerSerializer.Formatters;

internal sealed class ReferenceFormatter<T> : IFormatter<T?> where T : class
{
    /// <summary>
    /// The formatter to use when serializing/deserializing the contents of type <c>T</c>.
    /// </summary>
    private readonly IFormatter<T> _valueFormatter;

    internal ReferenceFormatter(IFormatter<T> valueFormatter)
    {
        _valueFormatter = valueFormatter;
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T? value)
    {
        // todo: this control flow feels weird and gross in my head

        var reference = Reference.Read(reader);
        var holder = reader.Context.GetOrCreateReference<T>(reference);

        if (holder is null)
        {
            value = null;
            return;
        }
        
        if (reference == Reference.New)
        {
            _valueFormatter.Deserialize(reader, out holder.Value);
        }

        // Todo: assert that holder has been initialized by the extant reference. if not, throw helpful exception
        value = holder.Value;
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T? value)
    {
        var reference = writer.Context.RecordReference(value);
        reference.Write(writer);

        if (reference == Reference.New)
        {
            _valueFormatter.Serialize(writer, value!);
        }
    }
}
