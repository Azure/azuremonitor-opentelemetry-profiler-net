// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartupTests;

public class DepsFileTelemetryStackDetectorTests
{
    private const string OpenTelemetryDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "OpenTelemetry/1.9.0": {}, "OpenTelemetry.Extensions.Hosting/1.9.0": {} } }
        """;

    private const string AzureMonitorDistroDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Azure.Monitor.OpenTelemetry.AspNetCore/1.2.0": {}, "OpenTelemetry/1.9.0": {} } }
        """;

    // Legacy classic Application Insights SDK 2.x (also transitively references the OpenTelemetry SDK).
    private const string ApplicationInsights2xDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Microsoft.ApplicationInsights.AspNetCore/2.23.0": {}, "Microsoft.ApplicationInsights/2.23.0": {}, "OpenTelemetry/1.15.3": {} } }
        """;

    // Current OpenTelemetry-based Application Insights SDK 3.x.
    private const string ApplicationInsights3xDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Microsoft.ApplicationInsights.AspNetCore/3.1.2": {}, "Microsoft.ApplicationInsights/3.1.2": {}, "Azure.Monitor.OpenTelemetry.Exporter/1.8.0": {}, "OpenTelemetry/1.15.3": {} } }
        """;

    private const string WorkerService2xDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Microsoft.ApplicationInsights.WorkerService/2.23.0": {}, "Microsoft.ApplicationInsights/2.23.0": {} } }
        """;

    private const string WorkerService3xDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Microsoft.ApplicationInsights.WorkerService/3.1.2": {}, "OpenTelemetry/1.15.3": {} } }
        """;

    private const string NeitherDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Newtonsoft.Json/13.0.3": {} } }
        """;

    private const string IsolatedFunctionsWorkerDeps = """
        { "libraries": { "EventHubProfiler_Function/1.0.0": {}, "Microsoft.Azure.Functions.Worker.OpenTelemetry/1.2.0": {}, "Azure.Monitor.OpenTelemetry.Exporter/1.7.0": {}, "OpenTelemetry.Extensions.Hosting/1.15.3": {}, "OpenTelemetry/1.15.3": {} } }
        """;

    // The Functions platform host can carry classic Application Insights 2.x. Its host-only package marker
    // must take precedence so site-global injection never activates the classic profiler in this process.
    private const string AzureFunctionsPlatformHostDeps = """
        { "libraries": { "Microsoft.Azure.WebJobs.Script.WebHost/4.1045.200": {}, "Microsoft.ApplicationInsights.AspNetCore/2.23.0": {}, "Microsoft.ApplicationInsights/2.23.0": {}, "OpenTelemetry/1.15.3": {} } }
        """;

    // App already references the OpenTelemetry profiler NuGet (note its .Core dependency is also present).
    private const string OtelProfilerReferencedDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Azure.Monitor.OpenTelemetry.Profiler/1.0.0-beta2": {}, "Azure.Monitor.OpenTelemetry.Profiler.Core/1.0.0-beta2": {}, "OpenTelemetry/1.9.0": {} } }
        """;

    // App already references the classic Application Insights profiler NuGet.
    private const string ClassicProfilerReferencedDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Microsoft.ApplicationInsights.Profiler.AspNetCore/2.7.0": {}, "Microsoft.ApplicationInsights.AspNetCore/2.23.0": {} } }
        """;

    // Only the profiler's .Core dependency is present (not the public package) - must NOT be treated as
    // already-referenced; falls through to normal detection (OpenTelemetry here).
    private const string OtelProfilerCoreOnlyDeps = """
        { "libraries": { "SampleApp/1.0.0": {}, "Azure.Monitor.OpenTelemetry.Profiler.Core/1.0.0": {}, "OpenTelemetry/1.9.0": {} } }
        """;

    [Theory]
    [InlineData(OpenTelemetryDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(AzureMonitorDistroDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(ApplicationInsights2xDeps, TelemetryStack.LegacyApplicationInsights)]
    [InlineData(ApplicationInsights3xDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(WorkerService2xDeps, TelemetryStack.LegacyApplicationInsights)]
    [InlineData(WorkerService3xDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(NeitherDeps, TelemetryStack.None)]
    [InlineData(OtelProfilerReferencedDeps, TelemetryStack.AlreadyInstrumented)]
    [InlineData(ClassicProfilerReferencedDeps, TelemetryStack.AlreadyInstrumented)]
    [InlineData(OtelProfilerCoreOnlyDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(IsolatedFunctionsWorkerDeps, TelemetryStack.OpenTelemetry)]
    [InlineData(AzureFunctionsPlatformHostDeps, TelemetryStack.AzureFunctionsPlatformHost)]
    internal void DetectFromDepsJson_ClassifiesStack(string depsJson, TelemetryStack expected)
    {
        Assert.Equal(expected, DepsFileTelemetryStackDetector.DetectFromDepsJson(depsJson));
    }

    [Fact]
    internal void DetectFromDepsJson_WhenNoLibrariesSection_ReturnsNone()
    {
        Assert.Equal(TelemetryStack.None, DepsFileTelemetryStackDetector.DetectFromDepsJson("{}"));
    }

    [Fact]
    internal void DetectFromDepsJson_WhenMalformed_ReturnsNone()
    {
        Assert.Equal(TelemetryStack.None, DepsFileTelemetryStackDetector.DetectFromDepsJson("not json"));
    }

    [Fact]
    internal void Detect_WhenDepsPathNull_ReturnsNone()
    {
        DepsFileTelemetryStackDetector detector = new(depsFilePathProvider: () => null, readAllText: _ => null);
        Assert.Equal(TelemetryStack.None, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenNoDepsJson_AsNonDotNetApp_ReturnsNone()
    {
        // A non-.NET App Service app (Node.js/Python/Java/PHP) has no managed entry assembly and thus no
        // *.deps.json, so detection yields None and the profiler is never activated - a safe no-op.
        DepsFileTelemetryStackDetector detector = new(depsFilePathProvider: () => null, readAllText: _ => null);
        Assert.Equal(TelemetryStack.None, detector.Detect());
    }

    [Fact]
    internal void Detect_ReadsAndClassifiesProvidedDeps()
    {
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => @"C:\app\SampleApp.deps.json",
            readAllText: _ => OpenTelemetryDeps);

        Assert.Equal(TelemetryStack.OpenTelemetry, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenEntryAssemblyIsAzureFunctionsPlatformHost_SuppressesWithoutDeps()
    {
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => null,
            readAllText: _ => null,
            entryAssemblyNameProvider: () => "Microsoft.Azure.WebJobs.Script.WebHost",
            processIdProvider: () => 4242);

        Assert.Equal(TelemetryStack.AzureFunctionsPlatformHost, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenIsolatedWorkerEntryAssembly_ClassifiesWorkerDeps()
    {
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => @"C:\home\site\wwwroot\EventHubProfiler_Function.deps.json",
            readAllText: _ => IsolatedFunctionsWorkerDeps,
            entryAssemblyNameProvider: () => "EventHubProfiler_Function",
            processIdProvider: () => 4243);

        Assert.Equal(TelemetryStack.OpenTelemetry, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenNoSdkButAppServiceAiAgentPresent_ReturnsAgentInstrumentedNoSdk()
    {
        // Models the App Service pre-installed App Insights agent instrumenting the app at runtime while the
        // app references no supported SDK in its build.
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => @"C:\app\SampleApp.deps.json",
            readAllText: _ => NeitherDeps,
            environmentVariableProvider: name => name == "ApplicationInsightsAgent_EXTENSION_VERSION" ? "~3" : null);

        Assert.Equal(TelemetryStack.AgentInstrumentedNoSdk, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenNoSdkAndNoAgent_ReturnsNone()
    {
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => @"C:\app\SampleApp.deps.json",
            readAllText: _ => NeitherDeps,
            environmentVariableProvider: _ => null);

        Assert.Equal(TelemetryStack.None, detector.Detect());
    }

    [Fact]
    internal void Detect_WhenSupportedSdkAndAgentPresent_SdkWins()
    {
        // A referenced supported SDK takes precedence over the agent overlay (the profiler activates normally).
        DepsFileTelemetryStackDetector detector = new(
            depsFilePathProvider: () => @"C:\app\SampleApp.deps.json",
            readAllText: _ => OpenTelemetryDeps,
            environmentVariableProvider: _ => "~3");

        Assert.Equal(TelemetryStack.OpenTelemetry, detector.Detect());
    }
}
