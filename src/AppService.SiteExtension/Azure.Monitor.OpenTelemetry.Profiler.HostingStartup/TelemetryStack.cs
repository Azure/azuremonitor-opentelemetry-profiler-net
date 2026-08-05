// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

/// <summary>
/// The telemetry stack an application is built against, used to decide whether/how to enable the profiler.
/// </summary>
internal enum TelemetryStack
{
    /// <summary>No supported telemetry stack detected.</summary>
    None,

    /// <summary>
    /// A supported OpenTelemetry-based stack: the Azure Monitor OpenTelemetry distro, the current
    /// OpenTelemetry-based Application Insights SDK (3.x), or a manual OpenTelemetry setup. Enabled via
    /// the Azure Monitor OpenTelemetry profiler.
    /// </summary>
    OpenTelemetry,

    /// <summary>
    /// The legacy classic Application Insights SDK (2.x). This profiler does not support it; use the
    /// Application Insights Profiler for ASP.NET Core instead.
    /// </summary>
    LegacyApplicationInsights,

    /// <summary>
    /// The application already references an EventPipe profiler NuGet package
    /// (<c>Azure.Monitor.OpenTelemetry.Profiler</c> or <c>Microsoft.ApplicationInsights.Profiler.AspNetCore</c>)
    /// and therefore activates the profiler in its own code. Codeless enablement backs off to avoid double
    /// activation (a second EventPipe session).
    /// </summary>
    AlreadyInstrumented,

    /// <summary>
    /// No supported SDK is referenced in the app's build (<c>*.deps.json</c>), but the App Service
    /// pre-installed Application Insights codeless agent is instrumenting the app at runtime. The profiler
    /// cannot be enabled against the agent's repacked, below-floor classic SDK, so codeless enablement does
    /// not activate and instead logs a recommendation to add a supported SDK NuGet package.
    /// </summary>
    AgentInstrumentedNoSdk,

    /// <summary>
    /// The current process is the Azure Functions platform host for an out-of-process worker app, not the
    /// customer's worker. Site-global injection reaches both processes, so codeless enablement intentionally
    /// does nothing in this process and lets a supported .NET isolated worker perform its own detection.
    /// </summary>
    AzureFunctionsPlatformHost,
}
