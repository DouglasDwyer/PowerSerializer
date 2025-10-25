using System;
using System.Linq;

namespace DouglasDwyer.PowerSerializer;

/// <summary>
/// Utility class for solving generic type equations. That is, answering questions like:
/// "what substitution for <c>K</c> and <c>V</c> makes <c>Dictionary&lt;K, V&gt;</c> equal to <c>Dictionary&lt;string, int&gt;</c>?"
/// </summary>
internal struct GenericEquation
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
    public GenericEquation(Type[] parameters)
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
