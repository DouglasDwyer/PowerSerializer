using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DouglasDwyer.PowerSerializer.Formatters;

internal class BlitFormatter<T> : IFormatter<T> where T : unmanaged
{
    public BlitFormatter()
    {
        // todo: assert that T is ACTUALLY blittable (i.e. unmanaged, no booleans, little endian, explicit layout, no gaps)
        // tbh would also want to check that the fields are publicly constructible:
        // i.e. this should be an optimization for DynamicFormatter and not enable serialization of additional types.

        if (!BitConverter.IsLittleEndian)
        {
            
        }
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out T value)
    {
        value = MemoryMarshal.Read<T>(reader.Read(Marshal.SizeOf<T>()));
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in T value)
    {
        writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
    }

    /*
    private static bool IsBlittable(Type type)
    {
        if (!type.IsValueType)
        {
            return false;
        }

        if (type.IsPrimitive)
        {
            if (type == typeof(bool))
            {
                return false;
            }
            if (1 < Marshal.SizeOf(type) && !BitConverter.IsLittleEndian)
            {
                return false;
            }

            return true;
        }

        if (!type.IsLayoutSequential && !type.IsExplicitLayout)
        {
            return false;
        }

        var bytesTaken = new BitArray(Marshal.SizeOf(type));

        // Note: this should only enumerate the fields that are accessible to us.
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (!IsBlittable(field.FieldType))
            {
                return false;
            }

            var start = (int)Marshal.OffsetOf(type, field.Name);
            var end = start + Marshal.SizeOf(field.FieldType);
            for (var i = start; i < end; i++)
            {
                bytesTaken.Set(i, true);
            }
        }

        return bytesTaken.HasAllSet();
    }*/
}
