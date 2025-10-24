namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// A formatter factory that defines how to serialize a (potentially dynamic) set of types.
/// </summary>
public interface IFormatterResolver
{
    /// <summary>
    /// Constructs a formatter for use with the given serializer.
    /// </summary>
    /// <typeparam name="T">The exact type to be serialized.</typeparam>
    /// <param name="serializer">The serializer object.</param>
    /// <returns>
    /// A formatter for objects of exact type <typeparamref name="T"/>, or <c>null</c>
    /// if the type is not supported.
    /// </returns>
    IFormatter<T>? GetFormatter<T>(PowerSerializer serializer);
}
