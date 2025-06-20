using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// Detects the role name.
/// This is used to determine the role name for the application by 1 method. A collection of role name detectors 
/// can be registered in the ServiceCollection and the first one that returns a non-null value will be used.
/// See <see cref="AggregatedRoleNameSource"/> for the usage.
/// </summary>
internal interface IRoleNameDetector
{
    /// <summary>
    /// Gets the role name. Returns <see cref="String.Empty"/> if the role name cannot be determined.
    /// </summary>
    string? GetRoleName();
}