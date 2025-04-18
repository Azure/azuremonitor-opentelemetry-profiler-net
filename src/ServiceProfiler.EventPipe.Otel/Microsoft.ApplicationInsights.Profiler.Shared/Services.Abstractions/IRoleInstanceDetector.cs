namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// Detects the role instance.
/// This is used to determine the role instance for the application by 1 method. A collection of role instance detectors 
/// can be registered in the ServiceCollection and the first one that returns a non-null value will be used.
/// </summary>
internal interface IRoleInstanceDetector
{
    /// <summary>
    /// Gets the role instance. Returns <see cref="String.Empty"/> if the role name cannot be determined.
    /// </summary>
    string? GetRoleInstance();
}