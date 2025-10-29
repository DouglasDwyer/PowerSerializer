using System;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DouglasDwyer.PowerSerializer.Formatters;

/*
/// <summary>
/// Serializes method references by their name and signature.
/// </summary>
public sealed class MethodBaseFormatter : IFormatter<MethodBase>
{
    /// <summary>
    /// A reference serializer that will recursively fall back to this
    /// <see cref="MethodBaseFormatter"/> when it encounters a new type.
    /// </summary>
    private readonly IFormatter<MethodBase?> _methodReferenceFormatter;

    /// <summary>
    /// Formats method references. Used when serializing the parameter types
    /// of methods.
    /// </summary>
    private readonly IFormatter<Type?> _typeFormatter;

    public MethodBaseFormatter(PowerSerializer serializer)
    {
        _methodReferenceFormatter = serializer.GetFormatter<MethodBase>();
        _typeFormatter = serializer.GetFormatter<Type>();
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out MethodBase value)
    {
        var isConstructedGeneric = reader.ReadBool();

        if (isConstructedGeneric)
        {
            _methodReferenceFormatter.Deserialize(reader, out var definition);
            var genericArguments = new Type[definition!.GetGenericArguments().Length];

            for (var i = 0; i < genericArguments.Length; i++)
            {
                _typeFormatter.Deserialize(reader, out genericArguments[i]!);
            }

            value = ((MethodInfo)definition!).MakeGenericMethod(genericArguments);
        }
        else
        {
            value = OpenGenericPlaceholder.Instance;

            var name = reader.ReadString();
            var genericParameterCount = reader.ReadUInt8();
            var parameterCount = reader.ReadUInt8();


        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in MethodBase value)
    {
        var genericArguments = value.IsGenericMethod ? value.GetGenericArguments() : Array.Empty<Type>();

        if (value.IsConstructedGenericMethod)
        {
            writer.WriteBool(false);
            _methodReferenceFormatter.Serialize(writer, ((MethodInfo)value).GetGenericMethodDefinition());

            for (var i = 0; i < genericArguments.Length; i++)
            {
                _typeFormatter.Serialize(writer, genericArguments[i]);
            }
        }
        else
        {
            writer.WriteBool(false);
            writer.WriteString(value.Name);
            writer.WriteUInt8((byte)genericArguments.Length);

            var parameters = value.GetParameters();
            writer.WriteUInt8((byte)parameters.Length);
            for (var i = 0; i < parameters.Length; i++)
            {
                _typeFormatter.Serialize(writer, parameters[i].ParameterType);
            }
        }
    }

    internal sealed class OpenGenericPlaceholder : MethodBase
    {
        public static readonly OpenGenericPlaceholder Instance = new OpenGenericPlaceholder();

        /// <inheritdoc/>
        public override MethodAttributes Attributes => throw new NotImplementedException();

        /// <inheritdoc/>
        public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();

        /// <inheritdoc/>
        public override Type? DeclaringType => throw new NotImplementedException();

        /// <inheritdoc/>
        public override MemberTypes MemberType => throw new NotImplementedException();

        /// <inheritdoc/>
        public override string Name => throw new NotImplementedException();

        /// <inheritdoc/>
        public override Type? ReflectedType => throw new NotImplementedException();

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(bool inherit) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override MethodImplAttributes GetMethodImplementationFlags() => throw new NotImplementedException();

        /// <inheritdoc/>
        public override ParameterInfo[] GetParameters() => throw new NotImplementedException();

        /// <inheritdoc/>
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => throw new NotImplementedException();

        /// <inheritdoc/>
        public override bool IsDefined(Type attributeType, bool inherit) => throw new NotImplementedException();
    }
}
*/