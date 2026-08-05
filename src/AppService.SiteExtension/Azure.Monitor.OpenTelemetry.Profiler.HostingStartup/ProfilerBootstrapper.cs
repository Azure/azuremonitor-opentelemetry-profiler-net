// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(ProfilerBootstrapper))]

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

/// <summary>
/// Codeless entry point activated by the ASP.NET Core runtime when this assembly is listed in
/// <c>ASPNETCORE_HOSTINGSTARTUPASSEMBLIES</c>. It routes to the profiler that matches the application's
/// telemetry stack without any code change in the target application.
///
/// This router is deliberately <b>stack-agnostic</b>: it has no compile-time reference to either profiler
/// stack. It reflectively invokes a per-stack <em>activator</em> (via
/// <see cref="IProfilerActivatorInvoker"/>) that lives in its own payload subfolder with its own dependency
/// closure. Combined with the <c>StartupHook</c> scoping the assembly resolver to just the detected stack's
/// subfolder, this keeps the two stacks' dependencies fully isolated from each other.
///
/// Activation is fail-safe: every failure - during detection, during the deferred
/// <see cref="IWebHostBuilder.ConfigureServices"/> callback that registers the profiler (e.g. a
/// <c>MissingMethodException</c>/<c>FileNotFoundException</c> from an incompatible dependency version the
/// application brought), or later - is caught and logged so the host always starts. The profiler simply
/// disables itself; it never takes down the application.
/// </summary>
public sealed class ProfilerBootstrapper : IHostingStartup
{
    private readonly ITelemetryStackDetector _detector;
    private readonly IProfilerActivatorInvoker _activatorInvoker;
    private readonly IDependencyFloorChecker _floorChecker;

    public ProfilerBootstrapper()
        : this(new DepsFileTelemetryStackDetector(), new ReflectionProfilerActivatorInvoker(), new PayloadDependencyFloorChecker())
    {
    }

    internal ProfilerBootstrapper(
        ITelemetryStackDetector detector,
        IProfilerActivatorInvoker activatorInvoker,
        IDependencyFloorChecker? floorChecker = null)
    {
        _detector = detector ?? throw new ArgumentNullException(nameof(detector));
        _activatorInvoker = activatorInvoker ?? throw new ArgumentNullException(nameof(activatorInvoker));
        _floorChecker = floorChecker ?? NoDependencyFloorChecker.Instance;
    }

    /// <inheritdoc />
    public void Configure(IWebHostBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        try
        {
            TelemetryStack stack = ResolveStack();
            Apply(builder, stack);
        }
        catch (Exception ex)
        {
            BootstrapLog.Error("Codeless profiler bootstrap failed; the application will start without the profiler.", ex);
        }
    }

    /// <summary>
    /// Determines the stack to activate. Prefers the decision the <c>StartupHook</c> already recorded (it
    /// also scoped the assembly resolver to that stack's payload subfolder, so activating a different stack
    /// here would fail to resolve). Falls back to detecting locally when the hook did not run - e.g. in unit
    /// tests, or if <c>DOTNET_STARTUP_HOOKS</c> was not applied.
    /// </summary>
    private TelemetryStack ResolveStack()
    {
        if (AppContext.GetData(DetectedStackAppContextData.Key) is string recorded
            && Enum.TryParse(recorded, out TelemetryStack stack))
        {
            return stack;
        }

        return _detector.Detect();
    }

    /// <summary>
    /// Applies the routing decision. A supported OpenTelemetry-based stack enables the Azure Monitor
    /// OpenTelemetry profiler; the legacy classic Application Insights SDK (2.x) enables the classic
    /// Application Insights profiler; anything else enables nothing.
    ///
    /// The profiler-registration work runs inside a deferred <c>ConfigureServices</c> callback (executed
    /// later, during the application's host build) guarded by <see cref="TryActivate"/>: if the profiler
    /// cannot be registered - most importantly when the application brought an incompatible (or below-floor)
    /// version of a shared dependency (OpenTelemetry, the Application Insights SDK, Microsoft.Extensions.*,
    /// Azure.Core) than the activator was compiled against - the failure is logged and the application starts
    /// WITHOUT the profiler instead of crashing.
    /// </summary>
    internal void Apply(IWebHostBuilder builder, TelemetryStack stack)
    {
        switch (stack)
        {
            case TelemetryStack.OpenTelemetry:
                BootstrapLog.Info("Detected a supported OpenTelemetry-based telemetry stack. Enabling the Azure Monitor OpenTelemetry profiler.");
                builder.ConfigureServices(services => TryActivate(stack, services));
                break;

            case TelemetryStack.LegacyApplicationInsights:
                BootstrapLog.Info("Detected the classic Application Insights SDK (2.x). Enabling the classic Application Insights profiler.");
                builder.ConfigureServices(services => TryActivate(stack, services));
                break;

            case TelemetryStack.AlreadyInstrumented:
                BootstrapLog.Info("The application already references an Azure Monitor profiler NuGet package (Azure.Monitor.OpenTelemetry.Profiler or Microsoft.ApplicationInsights.Profiler.AspNetCore); codeless enablement backs off to avoid double activation. The profiler is expected to be enabled by the application's own code.");
                break;

            case TelemetryStack.AgentInstrumentedNoSdk:
                BootstrapLog.Info("The App Service Application Insights agent is instrumenting this application at runtime, but the application does not reference a supported telemetry SDK in its build (*.deps.json), so the codeless profiler cannot be enabled against it. To enable the profiler, add the latest 'Microsoft.ApplicationInsights.AspNetCore' NuGet package (version 3.0 or later, which is OpenTelemetry-based) to the application and redeploy. The profiler will then activate automatically.");
                break;

            case TelemetryStack.AzureFunctionsPlatformHost:
                BootstrapLog.Info("This process is the Azure Functions platform host, not the customer's isolated worker. Codeless profiler activation is intentionally suppressed in this process; the customer worker is detected independently.");
                break;

            default:
                BootstrapLog.Error("No supported telemetry stack (OpenTelemetry, the Azure Monitor OpenTelemetry distro, the OpenTelemetry-based Application Insights SDK 3.x, or the classic Application Insights SDK 2.x) was detected. The profiler will NOT be activated.");
                break;
        }
    }

    /// <summary>
    /// Reflectively enables the profiler for <paramref name="stack"/>, swallowing any failure. This runs
    /// inside the application's host build, so it MUST NOT propagate - an unhandled exception here crashes
    /// the host. Isolating the reflective activator invocation (which is where an incompatible-dependency
    /// load failure surfaces) behind this guarded call keeps activation fail-safe.
    /// </summary>
    private void TryActivate(TelemetryStack stack, IServiceCollection services)
    {
        try
        {
            // Pre-flight: if the app has already loaded a shared dependency below the version the payload was
            // built against, activation would fail to bind. Back off with a specific, actionable log instead
            // of relying on the catch below to swallow a JIT-time load exception. Best-effort (only sees
            // already-loaded assemblies); the catch remains the backstop for anything that loads later.
            IReadOnlyList<DependencyFloorViolation> violations = _floorChecker.CheckLoadedAgainstPayloadFloors(stack);
            if (violations.Count > 0)
            {
                foreach (DependencyFloorViolation v in violations)
                {
                    BootstrapLog.Error($"Cannot enable the profiler: the application loaded {v.Name} {v.Loaded} but the profiler requires >= {v.Required}. Upgrade {v.Name} (or the telemetry SDK that brings it) and redeploy.");
                }

                BootstrapLog.Info("Codeless profiler activation skipped due to below-floor dependency versions (see errors above). The application continues to run normally.");
                return;
            }

            _activatorInvoker.Invoke(stack, services);
        }
        catch (Exception ex)
        {
            BootstrapLog.Error("Failed to enable the profiler; the application will start without it.", ex);
        }
    }
}
