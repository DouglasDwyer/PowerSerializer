using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// A type-erased <see cref="SealedReferenceFormatter{T}"/> that allows for changing the underlying value formatter.
/// </summary>
internal interface ISealedReferenceFormatter
{
    /// <summary>
    /// Initializes the underlying value formatter.
    /// </summary>
    /// <param name="formatter">The formatter to use.</param>
    void SetValueFormatter(object formatter);
}
