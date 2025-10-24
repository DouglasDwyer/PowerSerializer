using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

internal sealed class PolymorphicFormatter<T> : IFormatter<T> where T : notnull
{
    // todo: assert in constructor that T actually polymorphic (not valuetype or sealed or assy/type/memberinfo)

    /// <summary>
    /// Used to read/write the types of polymorphic objects.
    /// </summary>
    private readonly IFormatter<Type> _typeFormatter;

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        _typeFormatter.Deserialize(reader, out var type);
        // get actual formatter from Ctx and do it
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        var type = value.GetType();
        _typeFormatter.Serialize(writer, type);
        // Current thinking is to pass it to the context.
        // Have a virtual function of the form Serialize(BufferWriter writer, object T)
        // on the format holders and invoke that on the proper one, which should perform the cast.
        // get actual formatter from Ctx and do it
        throw new NotImplementedException();
    }
}

internal abstract class PolymorphicFormatterBase
{
    protected abstract object Deserialize(BufferReader reader);
    protected abstract void Serialize(BufferWriter writer, object value);
}

internal sealed class PolymorphicFormatter1<T> : PolymorphicFormatterBase where T : class
{
    private readonly IFormatter<T> _inner;

    protected override object Deserialize(BufferReader reader)
    {
        ref var value = ref reader.Context.AddClass<T>();
        _inner.Deserialize(reader, out value);
        if (value is null)
        {
            throw new Exception("Expected non-null value from deserialization");
        }
        return value;
    }

    protected override void Serialize(BufferWriter writer, object value)
    {
        writer.Context.RecordReference(value);
        _inner.Serialize(writer, (T)value);
    }
}

internal sealed class PolymorphicFormatter2<T> : PolymorphicFormatterBase where T : struct
{
    private readonly IFormatter<T> _inner;

    protected override object Deserialize(BufferReader reader)
    {
        ref var value = ref reader.Context.AddBoxedValueType<T>();
        _inner.Deserialize(reader, out value);
        throw new NotImplementedException("now get the damn thing as an object...");
    }

    protected override void Serialize(BufferWriter writer, object value)
    {
        writer.Context.RecordReference(value);
        _inner.Serialize(writer, (T)value);
    }
}
