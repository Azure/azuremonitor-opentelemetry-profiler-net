// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

/// <summary>
/// Detects the telemetry stack by inspecting the host application's own <c>*.deps.json</c> file.
/// This reflects the packages the developer chose to reference and is unaffected by the profiler
/// assemblies we bundle alongside it (those live in a separate folder), so it cleanly distinguishes the
/// app's telemetry choice from our payload.
/// </summary>
/// <remarks>
/// The detection deliberately performs a lightweight text scan instead of parsing with
/// <c>System.Text.Json</c>. This code runs extremely early (while the host is being built) and must not
/// force-load a specific <c>System.Text.Json</c> version - the bundled profiler stack may reference a
/// newer version than the target application, which would fail to bind at this point.
/// </remarks>
internal sealed class DepsFileTelemetryStackDetector : ITelemetryStackDetector
{
    private const string AzureFunctionsHostAssembly = "Microsoft.Azure.WebJobs.Script.WebHost";

    // Set by the App Service pre-installed Application Insights codeless agent (DiagnosticServices). Its
    // presence means telemetry is being instrumented at RUNTIME by the agent - which our build-time
    // *.deps.json scan cannot see - so an otherwise-undetected app is still emitting telemetry.
    private const string AppServiceAiAgentEnvVar = "ApplicationInsightsAgent_EXTENSION_VERSION";

    private readonly Func<string?> _depsFilePathProvider;
    private readonly Func<string, string?> _readAllText;
    private readonly Func<string, string?> _environmentVariableProvider;
    private readonly Func<string?> _entryAssemblyNameProvider;
    private readonly Func<int> _processIdProvider;

    public DepsFileTelemetryStackDetector()
        : this(
            GetEntryAssemblyDepsPath,
            TryReadAllText,
            entryAssemblyNameProvider: GetEntryAssemblyName,
            processIdProvider: BootstrapLog.GetProcessId)
    {
    }

    internal DepsFileTelemetryStackDetector(
        Func<string?> depsFilePathProvider,
        Func<string, string?> readAllText,
        Func<string, string?>? environmentVariableProvider = null,
        Func<string?>? entryAssemblyNameProvider = null,
        Func<int>? processIdProvider = null)
    {
        _depsFilePathProvider = depsFilePathProvider ?? throw new ArgumentNullException(nameof(depsFilePathProvider));
        _readAllText = readAllText ?? throw new ArgumentNullException(nameof(readAllText));
        _environmentVariableProvider = environmentVariableProvider ?? Environment.GetEnvironmentVariable;
        _entryAssemblyNameProvider = entryAssemblyNameProvider ?? GetEntryAssemblyName;
        _processIdProvider = processIdProvider ?? BootstrapLog.GetProcessId;
    }

    public TelemetryStack Detect()
    {
        TelemetryStack stack = DetectFromDeps();

        // If nothing was detected from the build (*.deps.json), the app may still be instrumented at runtime
        // by the App Service Application Insights codeless agent. We cannot enable the profiler against that
        // (it injects a repacked, below-floor classic SDK), but we can surface an actionable recommendation
        // instead of a bare "no supported stack".
        if (stack == TelemetryStack.None && IsAppServiceAiAgentPresent())
        {
            return TelemetryStack.AgentInstrumentedNoSdk;
        }

        return stack;
    }

    private bool IsAppServiceAiAgentPresent() =>
        !string.IsNullOrEmpty(_environmentVariableProvider(AppServiceAiAgentEnvVar));

    private TelemetryStack DetectFromDeps()
    {
        string? entryAssemblyName = _entryAssemblyNameProvider();
        string? path = _depsFilePathProvider();
        BootstrapLog.Info(
            $"Telemetry detection context: PID={_processIdProvider()}, " +
            $"entry assembly='{entryAssemblyName ?? "<unknown>"}', " +
            $".deps.json='{path ?? "<not found>"}'.");

        // The site extension is injected at site scope, so an isolated Functions app runs this code in both
        // the platform host and the customer's worker. Suppress the host before inspecting its telemetry
        // dependencies; FUNCTIONS_* settings cannot be used because the customer worker inherits them too.
        if (IsAzureFunctionsPlatformHost(entryAssemblyName))
        {
            BootstrapLog.Info("Detected the Azure Functions platform host process; profiler activation is intentionally suppressed in this process.");
            return TelemetryStack.AzureFunctionsPlatformHost;
        }

        if (string.IsNullOrEmpty(path))
        {
            BootstrapLog.Info("Could not locate the application's .deps.json; treating telemetry stack as undetected.");
            return TelemetryStack.None;
        }

        string? content = _readAllText(path!);
        if (string.IsNullOrEmpty(content))
        {
            BootstrapLog.Info($"The application's .deps.json at '{path}' was empty or unreadable; treating telemetry stack as undetected.");
            return TelemetryStack.None;
        }

        TelemetryStack stack = DetectFromDepsJson(content!);
        if (stack == TelemetryStack.AzureFunctionsPlatformHost)
        {
            BootstrapLog.Info("The selected .deps.json belongs to the Azure Functions platform host; profiler activation is intentionally suppressed in this process.");
        }

        return stack;
    }

    /// <summary>
    /// Classifies the telemetry stack from the package names/versions referenced in a <c>.deps.json</c>
    /// document using a dependency-free text scan.
    /// </summary>
    /// <remarks>
    /// Per the profiler's supported-SDK matrix: the Azure Monitor OpenTelemetry distro and the current
    /// OpenTelemetry-based Application Insights SDK (3.x) are supported (OpenTelemetry profiler), while the
    /// legacy classic Application Insights SDK (2.x) is not. Detection is version-aware because the classic
    /// 2.x and the OpenTelemetry-based 3.x share the <c>Microsoft.ApplicationInsights.AspNetCore</c> package
    /// id, and 3.x also transitively pulls in the OpenTelemetry SDK.
    /// </remarks>
    internal static TelemetryStack DetectFromDepsJson(string depsJson)
    {
        // 0. Back off in the Azure Functions platform host. Site-global injection also reaches the customer's
        //    isolated worker, whose own dependency graph is classified normally in that separate process.
        //    The exact host library key avoids suppressing customer workers that reference
        //    Microsoft.Azure.Functions.Worker or inherit FUNCTIONS_* environment variables.
        if (ContainsExactPackage(depsJson, AzureFunctionsHostAssembly))
        {
            return TelemetryStack.AzureFunctionsPlatformHost;
        }

        // 1. Back off if the app already references an EventPipe profiler NuGet - it activates the profiler in
        //    its own code, so codeless enablement must not activate a second time (double EventPipe session).
        //    Uses the exact "<pkg>/" library-key marker so "Azure.Monitor.OpenTelemetry.Profiler" does not
        //    match its dependency "Azure.Monitor.OpenTelemetry.Profiler.Core".
        if (ContainsExactPackage(depsJson, "Azure.Monitor.OpenTelemetry.Profiler")
            || ContainsExactPackage(depsJson, "Microsoft.ApplicationInsights.Profiler.AspNetCore"))
        {
            return TelemetryStack.AlreadyInstrumented;
        }

        // 2. Azure Monitor OpenTelemetry distro - unambiguous, supported OpenTelemetry signal.
        if (ContainsPackageToken(depsJson, "Azure.Monitor.OpenTelemetry.AspNetCore"))
        {
            return TelemetryStack.OpenTelemetry;
        }

        // 3. Application Insights ASP.NET Core / Worker Service SDK. 3.x is an OpenTelemetry-based wrapper
        //    (supported via the OpenTelemetry profiler); 2.x is the legacy classic SDK (not supported).
        //    Checked before the generic OpenTelemetry test below, which 3.x pulls in transitively.
        if (ContainsPackageToken(depsJson, "Microsoft.ApplicationInsights.AspNetCore")
            || ContainsPackageToken(depsJson, "Microsoft.ApplicationInsights.WorkerService"))
        {
            int major = 0;
            if (TryGetPackageMajorVersion(depsJson, "Microsoft.ApplicationInsights.AspNetCore", out int aspNetMajor))
            {
                major = aspNetMajor;
            }
            else if (TryGetPackageMajorVersion(depsJson, "Microsoft.ApplicationInsights.WorkerService", out int workerMajor))
            {
                major = workerMajor;
            }
            else if (TryGetPackageMajorVersion(depsJson, "Microsoft.ApplicationInsights", out int baseMajor))
            {
                major = baseMajor;
            }

            return major >= 3 ? TelemetryStack.OpenTelemetry : TelemetryStack.LegacyApplicationInsights;
        }

        // 4. Manual OpenTelemetry setup (SDK / hosting) without either the distro or the AI SDK.
        if (ContainsPackageToken(depsJson, "OpenTelemetry"))
        {
            return TelemetryStack.OpenTelemetry;
        }

        return TelemetryStack.None;
    }

    /// <summary>
    /// Returns true when the document contains a quoted JSON token that begins with the given package
    /// name (a library key such as <c>"OpenTelemetry/1.9.0"</c> or a dependency reference such as
    /// <c>"OpenTelemetry.Api"</c>).
    /// </summary>
    private static bool ContainsPackageToken(string depsJson, string package) =>
        depsJson.IndexOf("\"" + package, StringComparison.OrdinalIgnoreCase) >= 0;

    /// <summary>
    /// Returns true when the document contains an exact package library key of the form
    /// <c>"&lt;package&gt;/&lt;version&gt;"</c>. The trailing <c>/</c> ensures an exact package match rather
    /// than a prefix - e.g. <c>Azure.Monitor.OpenTelemetry.Profiler</c> does not match its dependency
    /// <c>Azure.Monitor.OpenTelemetry.Profiler.Core</c>.
    /// </summary>
    private static bool ContainsExactPackage(string depsJson, string package) =>
        depsJson.IndexOf("\"" + package + "/", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool IsAzureFunctionsPlatformHost(string? entryAssemblyName) =>
        string.Equals(entryAssemblyName, AzureFunctionsHostAssembly, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Extracts the major version from a <c>.deps.json</c> library key of the exact form
    /// <c>"&lt;package&gt;/&lt;version&gt;"</c> (e.g. <c>"Microsoft.ApplicationInsights.AspNetCore/3.1.2"</c>).
    /// The trailing <c>/</c> ensures an exact package match rather than a prefix (so
    /// <c>Microsoft.ApplicationInsights</c> does not match <c>Microsoft.ApplicationInsights.AspNetCore</c>).
    /// </summary>
    private static bool TryGetPackageMajorVersion(string depsJson, string package, out int major)
    {
        major = 0;
        string marker = "\"" + package + "/";
        int index = depsJson.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return false;
        }

        int start = index + marker.Length;
        int end = start;
        while (end < depsJson.Length && char.IsDigit(depsJson[end]))
        {
            end++;
        }

        return end > start && int.TryParse(depsJson.Substring(start, end - start), out major);
    }

    private static string? GetEntryAssemblyDepsPath()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            return null;
        }

        string? directory = null;
        string? location = entryAssembly.Location;
        if (!string.IsNullOrEmpty(location))
        {
            directory = Path.GetDirectoryName(location);
        }

        if (string.IsNullOrEmpty(directory))
        {
            directory = AppContext.BaseDirectory;
        }

        string? assemblyName = entryAssembly.GetName().Name;
        if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(assemblyName))
        {
            return null;
        }

        string candidate = Path.Combine(directory!, assemblyName + ".deps.json");
        return File.Exists(candidate) ? candidate : null;
    }

    private static string? GetEntryAssemblyName() => Assembly.GetEntryAssembly()?.GetName().Name;

    private static string? TryReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
