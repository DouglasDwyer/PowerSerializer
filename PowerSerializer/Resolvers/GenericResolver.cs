using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DouglasDwyer.PowerSerializer.Resolvers;

/// <summary>
/// Instantiates a single class of <see cref="IFormatter"/>. Both <b>non-generic</b> and <b>generic</b> types are supported.
/// For example, given a formatter such as <c>DictionaryFormatter&lt;K, V&gt; : IFormatter&lt;Dictionary&lt;K, V&gt;&gt;</c>,
/// this class will automatically solve for <c>K</c> and <c>V</c> to instantiate concrete instances of <c>DictionaryFormatter&lt;K, V&gt;</c>.
/// </summary>
public sealed class GenericResolver : IFormatterResolver
{
    /// <summary>
    /// Arguments to pass to the formatter constructor whenever a formatter is created.
    /// </summary>
    public IEnumerable<object?> ConstructorArguments => _constructorArguments;

    /// <summary>
    /// The non-generic or open generic type to be instantiated.
    /// </summary>
    public readonly Type Definition;

    /// <inheritdoc cref="ConstructorArguments"/>
    private readonly object?[] _constructorArguments;

    /// <summary>
    /// Interfaces implemented by <see cref="Definition"/>.
    /// </summary>
    private readonly Type[] _interfaces;

    /// <summary>
    /// An array containing the generic parameters to compute.
    /// </summary>
    private readonly Type[] _parameters;

    /// <summary>
    /// Creates a new generic type resolver.
    /// </summary>
    /// <param name="definition">The formatter type that will be instantiated.</param>
    /// <param name="constructorArguments">
    /// The arguments to provide to the constructor of <paramref name="definition"/> when creating new formatters.
    /// To create a formatter, this resolver will search for constructors with <see cref="PowerSerializer"/> as their first argument,
    /// and <paramref name="constructorArguments"/> as the remaining ones. If that fails, then this resolver will search for constructors
    /// with arguments matching <paramref name="constructorArguments"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If <paramref name="definition"/> was not a valid, solvable type definition.
    /// </exception>
    public GenericResolver(Type definition, params object?[] constructorArguments)
    {
        if (definition.ContainsGenericParameters && !definition.IsGenericTypeDefinition)
        {
            throw new ArgumentException("Expected non-generic type, open generic type, or fully instantiated generic type", nameof(definition));
        }

        Definition = definition;
        _constructorArguments = constructorArguments;
        _interfaces = definition.GetInterfaces();
        _parameters = definition.IsGenericTypeDefinition ? definition.GetGenericArguments() : Array.Empty<Type>();
    }

    /// <inheritdoc/>
    public IFormatter? GetFormatter(PowerSerializer serializer, Type type)
    {
        var targetInterface = typeof(IFormatter<>).MakeGenericType(type);
        var equation = new GenericEquationSolver(_parameters);

        foreach (var iface in _interfaces)
        {
            if (equation.Solve(targetInterface, iface, out var substitutions))
            {
                Type formatterType;
                try
                {
                    formatterType = Definition.IsGenericType ? Definition.MakeGenericType(substitutions) : Definition;
                }
                catch (ArgumentException)
                {
                    // Failed to construct the type
                    // This can occur if there are generic parameter constraints,
                    // which are not accounted for by the equation solver.
                    continue;
                }

                var constructors = formatterType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                var result = TryInvokeConstructor(constructors, GetConstructorArgumentsWithSerializer(serializer))
                    ?? TryInvokeConstructor(constructors, _constructorArguments.ToArray());

                if (result is not null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Adds the <paramref name="serializer"/> to the beginning of the constructor arguments list,
    /// and then returns a copy.
    /// </summary>
    /// <param name="serializer">The associated serializer.</param>
    /// <returns>The new arguments list.</returns>
    private object?[] GetConstructorArgumentsWithSerializer(PowerSerializer serializer)
    {
        var result = new object[1 + _constructorArguments.Length];
        result[0] = serializer;
        new ReadOnlySpan<object?>(_constructorArguments).CopyTo(new Span<object?>(result, 1, _constructorArguments.Length));
        return result;
    }

    /// <summary>
    /// Attempts to invoke a constructor that matches the given arguments.
    /// </summary>
    /// <param name="constructors">
    /// The constructors from the concrete type instantiated from <see cref="Definition"/>.
    /// </param>
    /// <param name="arguments">
    /// The arguments with which to call the constructor. These may be reorganized or modified
    /// according to the method binding rules.
    /// </param>
    /// <returns>
    /// The formatter that was created, or <c>null</c> if no constructor was found.
    /// </returns>
    private IFormatter? TryInvokeConstructor(ConstructorInfo[] constructors, object?[] arguments)
    {
        ConstructorInfo constructor;
        try
        {
            constructor = (ConstructorInfo)Type.DefaultBinder.BindToMethod(
                BindingFlags.CreateInstance,
                constructors,
                ref arguments,
                null,
                null,
                null,
                out _
            );
        }
        catch (MissingMethodException)
        {
            return null;
        }

        return (IFormatter)constructor.Invoke(arguments);
    }

    /// <summary>
    /// Utility class for solving generic type equations. That is, answering questions like:
    /// "what substitution for <c>K</c> and <c>V</c> makes <c>Dictionary&lt;K, V&gt;</c> equal to <c>Dictionary&lt;string, int&gt;</c>?"
    /// </summary>
    private struct GenericEquationSolver
    {
        /// <summary>
        /// An array containing the generic parameters to compute.
        /// </summary>
        private readonly Type[] _parameters;

        /// <summary>
        /// An array containing a concrete type substitution for each element in <see cref="_parameters"/>.
        /// </summary>
        private readonly Type?[] _substitutions;

        /// <summary>
        /// Creates a new generic equation solver with the given left-hand side.
        /// </summary>
        /// <param name="parameters">
        /// An array containing the generic parameters to compute.
        /// </param>
        public GenericEquationSolver(Type[] parameters)
        {
            _parameters = parameters;
            _substitutions = 0 < _parameters.Length ? new Type?[_parameters.Length] : Array.Empty<Type?>();
        }

        /// <summary>
        /// Finds the unique type substitution that will make the left-hand side equal to <paramref name="rhs"/>.
        /// </summary>
        /// <param name="lhs">The left-hand side of the equation.</param>
        /// <param name="rhs">The right-hand side of the equation.</param>
        /// <param name="result">The set of type arguments that, when applied to the left-hand side, will make it equal to <paramref name="rhs"/>.</param>
        /// <returns>Whether a valid substitution was found.</returns>
        public bool Solve(Type lhs, Type rhs, out Type[] result)
        {
            Array.Fill(_substitutions, null);

            if (FindSubstition(lhs, rhs) && AllSubstitutionsFound())
            {
                result = _substitutions!;
                return true;
            }
            else
            {
                result = Array.Empty<Type>();
                return false;
            }
        }

        /// <summary>
        /// Fills the <see cref="_substitutions"/> map with replacements for the left-hand type <see cref="_parameters"/>.
        /// </summary>
        /// <param name="lhs">The open generic type.</param>
        /// <param name="rhs">The concrete type to match.</param>
        /// <returns>
        /// Whether a solution was found.
        /// </returns>
        private bool FindSubstition(Type lhs, Type rhs)
        {
            if (lhs == rhs)
            {
                return true;
            }
            else if (lhs.IsGenericParameter && !rhs.IsGenericParameter)
            {
                return SetSubstitution(lhs, rhs);
            }
            else if (!lhs.IsGenericParameter && rhs.IsGenericParameter)
            {
                return SetSubstitution(rhs, lhs);
            }
            else if (lhs.IsArray && rhs.IsArray)
            {
                if (lhs.GetArrayRank() == rhs.GetArrayRank() && lhs.IsVariableBoundArray == rhs.IsVariableBoundArray)
                {
                    return FindSubstition(lhs.GetElementType()!, rhs.GetElementType()!);
                }
                else
                {
                    return false;
                }
            }
            else if (lhs.IsConstructedGenericType && rhs.IsConstructedGenericType && lhs.GetGenericTypeDefinition() == rhs.GetGenericTypeDefinition())
            {
                foreach (var (a, b) in lhs.GetGenericArguments().Zip(rhs.GetGenericArguments()))
                {
                    if (!FindSubstition(a, b))
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks that all parameters have a substitution assignment.
        /// </summary>
        /// <returns>
        /// True if all elements of <see cref="_substitutions"/> are non-null.
        /// </returns>
        private bool AllSubstitutionsFound()
        {
            for (var i = 0; i < _substitutions.Length; i++)
            {
                if (_substitutions[i] is null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the type parameter <paramref name="lhs"/> as being equal to <paramref name="rhs"/>.
        /// Returns <c>false</c> if a conflict occurred.
        /// </summary>
        /// <param name="lhs">The type parameter.</param>
        /// <param name="rhs">The concrete type to assign.</param>
        /// <returns>Whether the substitution is possible.</returns>
        private bool SetSubstitution(Type lhs, Type rhs)
        {
            for (var i = 0; i < _parameters.Length; i++)
            {
                if (lhs == _parameters[i])
                {
                    ref var substutiton = ref _substitutions[i];

                    if (substutiton is null)
                    {
                        substutiton = rhs;
                        return true;
                    }
                    else if (substutiton != rhs)
                    {
                        return false;
                    }
                }
            }

            throw new InvalidOperationException("First argument was not a type parameter in the map");
        }
    }
}
