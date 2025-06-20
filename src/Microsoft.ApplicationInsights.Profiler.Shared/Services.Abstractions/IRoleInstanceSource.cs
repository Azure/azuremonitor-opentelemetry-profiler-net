namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IRoleInstanceSource
{
    /// <summary>
    /// Gets the cloud role instance.
    /// </summary>
    string CloudRoleInstance { get; }
}