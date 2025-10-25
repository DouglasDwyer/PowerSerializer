using System;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Raised whenever <see cref="PowerSerializer"/> is unable to find a formatter for a type.
/// </summary>
public class MissingFormatterException : Exception
{
    /// <summary>
    /// The type that needed to be serialized or deserialized.
    /// </summary>
    public readonly Type Target;

    /// <summary>
    /// Creates a new exception.
    /// </summary>
    /// <param name="target">The type that needed to be serialized or deserialized.</param>
    public MissingFormatterException(Type target) : base($"Unable to find formatter for type {target}")
    {
        Target = target;
    }
}
