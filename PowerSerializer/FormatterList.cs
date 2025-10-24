using DouglasDwyer.PowerSerializer.Formatters;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DouglasDwyer.PowerSerializer;

// todo
public sealed class FormatterList
{
    /// <summary>
    /// Set of formatters for basic C# types - such as primitives, collections, and reflection metadata.
    /// </summary>
    public static readonly FormatterList Default = new FormatterList()
        .With(typeof(AttributeSpecifiedFormatter<>), typeof(DefaultFormatterAttribute))
        .With(typeof(ArrayFormatter<>))
        .With(typeof(AssemblyFormatter))
        .With(typeof(BlitArrayFormatter<>))
        .With(typeof(BlitFormatter<>))
        .With(typeof(PrimitiveFormatter))
        .With(typeof(TypeFormatter));

    /// <summary>
    /// Gets an iterator over all entries in this list.
    /// </summary>
    internal IEnumerable<Entry> Entries => _inner;

    /// <summary>
    /// The inner list of formatter objects.
    /// </summary>
    private readonly ImmutableList<Entry> _inner;

    /// <summary>
    /// Creates a new list, without any formatters. Use <see cref="Default"/>
    /// to get a predefined list with common formatters.
    /// </summary>
    public FormatterList() : this(ImmutableList<Entry>.Empty) { }

    /// <summary>
    /// Instantiates a formatter list.
    /// </summary>
    /// <param name="inner">The entries to include in the list.</param>
    private FormatterList(ImmutableList<Entry> inner)
    {
        _inner = inner;
    }

    /// <summary>
    /// Appends a formatter of <paramref name="type"/> to the end of the list.
    /// When resolving a new formatter, the list is searched for the first compatible
    /// type from <b>front to back</b>. Earlier formatters take priority.
    /// </summary>
    /// <param name="type">
    /// The formatter type to use. This can be a concrete type, like <c>typeof(FooFormatter)</c>
    /// or <c>typeof(BarFormatter&lt;Baz&gt;)</c>. This can also be an open generic, such as
    /// <c>typeof(DictionaryFormatter&lt;,&gt;)</c>. The type parameters will be matched at runtime
    /// to create serializer instances.
    /// </param>
    /// <param name="args">
    /// Any arguments that should be passed to the constructor during creation.
    /// </param>
    /// <returns>
    /// A copy of the list, with the formatter added.
    /// </returns>
    public FormatterList With(Type type, params ReadOnlySpan<object> args)
    {
        if (ImplementsIFormatter(type))
        {
            var entry = new Entry { ConstructorArguments = args.ToArray(), FormatterType = type };
            return new FormatterList(_inner.Add(entry));
        }
        else
        {
            throw new ArgumentException("Type did not implement the IFormatter<T> interface for at least one type", nameof(type));
        }
    }

    /// <summary>
    /// Appends the provided list of formatters to the end of this one.
    /// When resolving a new formatter, the list is searched for the first compatible
    /// type from <b>front to back</b>. Earlier formatters take priority.
    /// </summary>
    /// <param name="other">The list to append.</param>
    /// <returns>
    /// A copy of the list, with the formatters added.
    /// </returns>
    public FormatterList With(FormatterList other)
    {
        return new FormatterList(_inner.AddRange(other._inner));
    }

    /// <summary>
    /// Checks that <paramref name="type"/> implements at least one <see cref="IFormatter{T}"/> interface.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool ImplementsIFormatter(Type type)
    {
        return type.GetInterfaces()
            .Any(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(IFormatter<>));
    }

    /// <summary>
    /// Defines a particular formatter and how to construct it.
    /// </summary>
    internal struct Entry
    {
        /// <summary>
        /// The arguments to pass to the constructor, if any.
        /// </summary>
        public required object[] ConstructorArguments;

        /// <summary>
        /// The type of the formatter.
        /// </summary>
        public required Type FormatterType;

        public void Construct(object target, PowerSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}