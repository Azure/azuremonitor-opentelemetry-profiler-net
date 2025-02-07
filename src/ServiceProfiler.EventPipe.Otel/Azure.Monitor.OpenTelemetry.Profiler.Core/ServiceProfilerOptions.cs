using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

/// <summary>
/// Service Profiler Options
/// </summary>
public class ServiceProfilerOptions : UserConfigurationBase
{
    /// <summary>
    /// Gets or sets the connection string.
    /// </summary>
    public string? ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Get or sets the value of <see cref="TokenCredential" />.
    /// If <see cref="TokenCredential" /> is not set, AAD authentication is disabled
    /// and Instrumentation Key from the Connection String will be used.
    /// </summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/azure/azure-monitor/app/sdk-connection-string?tabs=net#is-the-connection-string-a-secret"/>.
    /// </remarks>
    public TokenCredential? Credential { get; set; }
}