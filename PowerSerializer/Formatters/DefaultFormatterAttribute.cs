using System;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Specifies the formatter that a type should use.
/// </summary>
public sealed class DefaultFormatterAttribute : FormatterBaseAttribute
{
    /// <inheritdoc cref="DefaultFormatterAttribute"/>
    /// <param name="formatter">The formatter to use on the type where this attribute is applied.</param>
    public DefaultFormatterAttribute(Type formatter) : base(formatter) { }
}