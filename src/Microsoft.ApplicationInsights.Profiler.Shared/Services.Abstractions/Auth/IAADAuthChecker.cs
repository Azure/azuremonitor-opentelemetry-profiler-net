namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;

/// <summary>
/// Checks if AAD Auth for application insights is enabled or not.
/// </summary>
internal interface IAADAuthChecker
{
    /// <summary>
    /// Checks if authentication is enabled.
    /// </summary>
    bool IsAADAuthenticateEnabled { get; }
}