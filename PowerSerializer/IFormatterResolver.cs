using DouglasDwyer.PowerSerializer.Resolvers;
using System;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// A dynamic provider of <see cref="IFormatter{T}"/> instances.
/// Resolvers facilitate the creation of a specialized formatter for each type.
/// For example, the <see cref="ArrayResolver"/> creates X instances for
/// each unique array and element type.
/// </summary>
public interface IFormatterResolver
{
    /// <summary>
    /// Gets the formatter for serializing the given concrete type, if possible.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <param name="type">The type to serialize.</param>
    /// <returns>
    /// The formatter to use, or <c>null</c> if this resolver does not support <paramref name="type"/>.
    /// </returns>
    IFormatter? GetFormatter(PowerSerializer serializer, Type type);
}
