using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal static class EnumerableUtilities
{
    /// <summary>
    /// Converts null to Enumerable.Empty{T}. The intention for this method is to simplify the caller by avoid null check for IEnumerable{T}.
    /// </summary>
    /// <typeparam name="T">The type to assign to the type parameter of the returned generic System.Collections.Generic.IEnumerable{T}.</typeparam>
    /// <param name="target">The original enumerable object. This could be null.</param>
    /// <returns>Returns the original IEnumerable{T} when it is not null. Or Enumerable.Empty{T} if it is null.</returns>
    /// <remarks>
    /// It is still preferred if the callee can return Enumerable.Empty{T}() than null.
    /// </remarks>
    public static IEnumerable<T> NullAsEmpty<T>(this IEnumerable<T> target)
        => target ?? Enumerable.Empty<T>();
}
