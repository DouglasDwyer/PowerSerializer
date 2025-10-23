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
