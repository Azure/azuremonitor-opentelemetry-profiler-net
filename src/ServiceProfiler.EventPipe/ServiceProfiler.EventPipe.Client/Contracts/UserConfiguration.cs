using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Core.Contracts;

/// <summary>
/// This is the data contract to accept user configurations from IConfigure. Ideally, every settings should be optional,
/// and default values should have been provided.
/// For example:
/// {
///     "ServiceProfiler": {
///         "IsDisabled": false,
///         "BufferSizeInMB": "250",
///         "Duration": "00:00:30",
///         "Interval": "00:29:30",
///         "InitialDelay": "00:00:10",
///         "ProvideAnonymousTelemetry": false,
///         "IsSkipCompatibilityTest": false,
///         "Endpoint": "https://agent.azureserviceprofiler.net",
///         "SkipEndpointCertificateValidation": false,
///         "PreserveTraceFile": false,
///         "LocalCacheFolder": "/tmp/",
///         "UploadMode": "OnSuccess",
///         "UploaderEnvironment": "Production",
///         "NamedPipe": {
///             "ConnectionTimeout": "00:00:30"
///         },
///         "TraceScavenger": {
///             "InitialDelay": "00:05:00"
///             "Interval": "00:15:00"
///         }
///     }
/// }
/// </summary>
public class UserConfiguration : UserConfigurationBase
{
    // Any configuration property here should be specific to Application Insights EventPipe Profiler and can not be shared.
}
