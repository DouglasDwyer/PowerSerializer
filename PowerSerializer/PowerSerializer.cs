using DouglasDwyer.PowerSerializer.Formatters;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace DouglasDwyer.PowerSerializer;

public sealed class PowerSerializer
{
    /// <summary>
    /// Where to find new assemblies during deserialization.
    /// </summary>
    private readonly AssemblyLoadContext _assemblyLoader;

    private readonly ConditionalWeakTable<Type, object> _formatters;

    public PowerSerializer()
    {
        _assemblyLoader = AssemblyLoadContext.GetLoadContext(Assembly.GetCallingAssembly())!;
        _formatters = new ConditionalWeakTable<Type, object>();
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
    public void Serialize<T>(IBufferWriter<byte> writer, in T value)
    {
        var context = SerializationContext.Pool.Get();
        try
        {
            GetFormatter<T>().Serialize(new BufferWriter(context, writer), value);
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
    /// <param name="data">A buffer containing the data produced during serialization.</param>
    /// <returns>The generated object.</returns>
    public T Deserialize<T>(ReadOnlySpan<byte> data)
    {
        var context = DeserializationContext.Pool.Get();
        try
        {
            var position = 0;
            GetFormatter<T>().Deserialize(new BufferReader(context, data, ref position), out var result);
            return result;
        }
        finally
        {
            DeserializationContext.Pool.Return(context);
        }
    }

    public IFormatter<T> GetFormatter<T>()
    {
        throw new Exception();
    }

    private sealed class TypeFormatData<T>
    {
        public readonly IFormatter<T> ReferenceFormatter;
        public readonly IFormatter<T> ValueFormatter;

        public TypeFormatData(IFormatter<T> valueFormatter, bool polymorphic)
        {

        }
    }

    /// <summary>
    /// Determines whether references to objects of <paramref name="type"/>
    /// require the <see cref="PolymorphicFormatter{T}"/>.
    /// </summary>
    /// <param name="type">The type in question.</param>
    /// <returns></returns>
    private static bool IsPolymorphic(Type type)
    {
        return !type.IsValueType
            && !type.IsSealed
            && !type.IsAssignableTo(typeof(Assembly))
            && !(type != typeof(MemberInfo) && type.IsAssignableTo(typeof(MemberInfo)));
    }
}