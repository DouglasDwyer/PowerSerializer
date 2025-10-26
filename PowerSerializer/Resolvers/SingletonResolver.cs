using System;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Whenever a <c>GetFormatter</c> call is compatible, returns a reference to a singleton formatter instance.
/// </summary>
public sealed class SingletonResolver : IFormatterResolver
{
    /// <summary>
    /// The formatter to return.
    /// </summary>
    public readonly IFormatter Instance;

    /// <summary>
    /// Resolves to the provided formatter.
    /// </summary>
    /// <param name="instance">The formatter to use.</param>
    public SingletonResolver(IFormatter instance)
    {
        Instance = instance;
    }

    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        if (Instance.GetType().IsAssignableTo(typeof(IFormatter<>).MakeGenericType(type)))
        {
            return Instance;
        }
        else
        {
            return null;
        }
    }
}
