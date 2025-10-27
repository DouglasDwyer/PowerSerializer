using FastExpressionCompiler;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Formatters;

/// <summary>
/// Serializes collection types that have either an <see cref="IComparer{T}"/> or <see cref="IEqualityComparer{T}"/> member.
/// </summary>
/// <typeparam name="K">The key type used for equality.</typeparam>
/// <typeparam name="T">The element type of the collection.</typeparam>
/// <typeparam name="A">The collection type itself.</typeparam>
public sealed class ComparerCollectionFormatter<K, T, A> : IFormatter<A> where A : ICollection<T?>
{
    private readonly IFormatter<object?> _comparerFormatter;

    /// <summary>
    /// Formatter for serializing list elements one at a time.
    /// </summary>
    private readonly IFormatter<T?> _elementFormatter;

    private readonly Func<int, object?, A> _newCollection;
    private readonly Func<A, object?> _getComparer;

    /*
     * Dictionary -> equalitycomparer
     HashSet -> equalitycomparer
    OrderedDictionary -> equalitycomparer
    PriorityQueue -> comparer
    SortedDictionary -> comparer
    SortedList -> comparer
    SortedSet -> comparer


     */

    public ComparerCollectionFormatter(PowerSerializer serializer)
    {
        var comparerProperty = typeof(A).GetProperty("Comparer");
        var constructor = typeof(A).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(int), typeof(IComparer<K>)])
            ?? typeof(A).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(IComparer<K>)])
            ?? typeof(A).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(int), typeof(IEqualityComparer<K>)])
            ?? typeof(A).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [typeof(IEqualityComparer<K>)]);

        if (comparerProperty is null)
        {
            throw new ArgumentException("Collection did not have Comparer property", nameof(A));
        }

        if (constructor is null)
        {
            throw new ArgumentException("Collection did not have capacity-comparer constructor", nameof(A));
        }

        if ((comparerProperty.PropertyType != typeof(IComparer<K>) && comparerProperty.PropertyType != typeof(IEqualityComparer<K>))
            || comparerProperty.GetMethod is null)
        {
            throw new ArgumentException("Collection comparer did not take elements of the specified type", nameof(K));
        }

        _comparerFormatter = serializer.GetFormatter<object>();
        _elementFormatter = serializer.GetFormatter<T>();
        
        _getComparer = (Func<A, object?>)Delegate.CreateDelegate(typeof(Func<A, object?>), comparerProperty.GetMethod);

        var countParam = Expression.Variable(typeof(int), "count");
        var comparerParam = Expression.Variable(typeof(object), "comparer");

        var castComparerParam = Expression.ConvertChecked(comparerParam, comparerProperty.PropertyType);
        var newExpression = constructor.GetParameters().Length == 2 ? Expression.New(constructor, countParam, castComparerParam) : Expression.New(constructor, castComparerParam);

        _newCollection = Expression.Lambda<Func<int, object?, A>>(newExpression, countParam, comparerParam)
            .CompileFast(false, CompilerFlags.DisableInterpreter | CompilerFlags.ThrowOnNotSupportedExpression);
    }

    /// <inheritdoc/>
    public void Deserialize(BufferReader reader, out A value)
    {
        var count = (int)reader.ReadVarUInt32();

        _comparerFormatter.Deserialize(reader, out var comparer);
        value = _newCollection(count, comparer);

        for (var i = 0; i < count; i++)
        {
            _elementFormatter.Deserialize(reader, out var element);
            value.Add(element);
        }
    }

    /// <inheritdoc/>
    public void Serialize(BufferWriter writer, in A value)
    {
        writer.WriteVarUInt32((uint)value.Count);

        _comparerFormatter.Serialize(writer, _getComparer(value));

        foreach (var element in value)
        {
            _elementFormatter.Serialize(writer, element);
        }
    }
}
