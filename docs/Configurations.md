# Service Profiler Configuration Guide

This document summarizes configuration options exposed via `UserConfigurationBase` (base for `ServiceProfilerOptions`). These are typically set when initializing the profiler in your application (e.g., through dependency injection or manual construction).

## Core Session Controls

| Property | Type | Default | Description |
|---------|------|---------|-------------|
| BufferSizeInMB | int | 250 | In-memory circular buffer size for trace data (approx). Not a cap on final trace file size. Increase only if traces truncate prematurely. |
| Duration | TimeSpan | 30s | Length of a single profiling session. Keep short in production to control overhead. |
| InitialDelay | TimeSpan | 0 | Delay before first profiling session. Useful to avoid startup noise. |
| ConfigurationUpdateFrequency | TimeSpan | 5s | Polling interval for updated triggers/remote settings. Raising reduces traffic; lowering increases responsiveness. |
| ActivatedOnStart | bool | true | If false, agent stays idle (fetching settings) until remote activation. |

## Enable / Disable Toggles

| Property | Default | Meaning |
|----------|---------|---------|
| IsDisabled | false | Master switch. When true, profiler does nothing. |
| IsSkipCompatibilityTest | false | Forces profiling even if environment checks fail. Use only for diagnostics. |
| StandaloneMode | false | Runs without contacting the backend service (no uploads, local only). |
| AllowsCrash | false | If true, unhandled exceptions in profiler surface (process may crash) to aid debugging. |

## Upload & Storage

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Endpoint | string? | null | Override service endpoint. Leave null for production default. |
| UploadMode | enum | OnSuccess | Options: Never, OnSuccess, Always. Use non-default only for debugging. |
| PreserveTraceFile | bool | false | Keeps local trace after successful upload. |
| LocalCacheFolder | string | OS temp path | Override temp directory for trace staging. |
| SkipEndpointCertificateValidation | bool | false | **Deprecated.** No longer supported by the ProfilerClient pipeline. |
| UploaderEnvironment | string | "Production" | **Deprecated.** Uploader warnings and errors are now always surfaced automatically. Use standard `Logging__LogLevel__` configuration for deeper diagnostics. |

## Random / Triggered Profiling

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| SamplingRate | double | 0.05 (5%) | Probability, evaluated once per scheduling cycle, that a random profiling session starts. On each cycle the profiler flips a coin: if `random < SamplingRate` it profiles for `Duration`; otherwise it stands by (~120s by default) before the next flip. Valid range `[0, 1]`. |
| CPUTriggerThreshold | float | 80 | Avg CPU (%) threshold to start a CPU-triggered session. See [CPU Usage Monitoring](CpuUsageMonitoring.md). |
| CPUTriggerCooldown | TimeSpan | (default per CpuTriggerSettings) | Minimum wait after a CPU-triggered session. |
| MemoryTriggerThreshold | float | 80 | Avg memory usage (%) threshold to start a memory-triggered session. See [Memory Usage Monitoring](MemoryUsageMonitoring.md). |
| MemoryTriggerCooldown | TimeSpan | (default per MemoryTriggerSettings) | Minimum wait after memory-triggered session. |

## Advanced / Extensibility

| Property | Type | Description |
|----------|------|-------------|
| CustomEventPipeProviders | IEnumerable&lt;EventPipeProviderItem&gt; | Adds extra EventPipe providers. Use cautiously—can greatly increase volume and overhead. |
| NamedPipe | NamedPipeOptions | IPC channel customization (diagnostics / tooling). |
| TraceScavenger | TraceScavengerServiceOptions | Controls cleanup policy for local traces (age, limits, etc.). |

## Telemetry & Privacy

| Property | Default | Notes |
|----------|---------|-------|
| ProvideAnonymousTelemetry | false | Enables anonymous usage telemetry to Microsoft. |

## Tuning Guidelines

1. Start with defaults.
2. Increase `BufferSizeInMB` only if logs show truncation or gaps.
3. Keep `Duration` ≤ 60s for production; shorter sessions reduce disruption.
4. Avoid changing `UploadMode` except during controlled troubleshooting.
5. Use triggers (CPU/Memory thresholds) to supplement, not replace, random sampling.
6. Set `StandaloneMode = true` only for offline/local investigations.

## Applying Configuration

The profiler binds its options from the **`ServiceProfiler`** configuration section. Every property above can therefore be set through any standard .NET configuration source — `appsettings.json`, environment variables, command-line arguments, or the programmatic callback. Values supplied through the `AddAzureMonitorProfiler(...)` callback are applied last and win over configuration sources.

### Example (Environment Variables)

.NET maps hierarchical configuration keys to environment variables by joining segments with a double underscore (`__`), prefixed with the section name `ServiceProfiler`:

```sh
# TimeSpan values use the "d.hh:mm:ss" / "hh:mm:ss" format
export ServiceProfiler__Duration="00:00:45"
export ServiceProfiler__InitialDelay="00:00:10"

# Numeric and boolean values
export ServiceProfiler__SamplingRate="0.1"
export ServiceProfiler__CPUTriggerThreshold="85"
export ServiceProfiler__MemoryTriggerThreshold="85"
export ServiceProfiler__PreserveTraceFile="true"
export ServiceProfiler__IsDisabled="false"

# String values
export ServiceProfiler__LocalCacheFolder="/var/profiler-cache"

# Array items are indexed with a numeric segment (0, 1, 2, ...)
export ServiceProfiler__CustomEventPipeProviders__0__Name="MyCompany-Diagnostics"
export ServiceProfiler__CustomEventPipeProviders__0__Level="4"
export ServiceProfiler__CustomEventPipeProviders__0__Keywords="0xFFFFFFFFFFFFFFFF"
```

On Windows PowerShell, set the same variables with `$env:`:

```powershell
$env:ServiceProfiler__Duration = "00:00:45"
$env:ServiceProfiler__SamplingRate = "0.1"
$env:ServiceProfiler__PreserveTraceFile = "true"
```

> Tip: In containers, pass these through your orchestrator (e.g., a Kubernetes `env:` list or a Docker `-e` flag) exactly as named above.

### Example (appsettings.json)

The same settings expressed as JSON:

```json
{
  "ServiceProfiler": {
    "Duration": "00:00:45",
    "InitialDelay": "00:00:10",
    "SamplingRate": 0.1,
    "CPUTriggerThreshold": 85,
    "MemoryTriggerThreshold": 85,
    "PreserveTraceFile": true,
    "IsDisabled": false,
    "LocalCacheFolder": "/var/profiler-cache",
    "CustomEventPipeProviders": [
      {
        "Name": "MyCompany-Diagnostics",
        "Level": 4,
        "Keywords": "0xFFFFFFFFFFFFFFFF"
      }
    ]
  }
}
```

## Example (Programmatic Setup)

```csharp
var options = new ServiceProfilerOptions
{
    Duration = TimeSpan.FromSeconds(45),
    SamplingRate = 0.1, // ~10% chance to start a session each cycle
    CPUTriggerThreshold = 85f,
    MemoryTriggerThreshold = 85f,
    PreserveTraceFile = true,
    LocalCacheFolder = "/var/profiler-cache",
    CustomEventPipeProviders = new []
    {
        new EventPipeProviderItem
        {
            Name = "MyCompany-Diagnostics",
            Level = 4, // Verbose
            Keywords = "0xFFFFFFFFFFFFFFFF"
        }
    }
};
```

## Frequency Estimation

Random profiling is scheduled with a per-cycle coin flip, so `SamplingRate`, `Duration`, and the standby duration together determine how often sessions run. Modeling each cycle as either a session (`Duration` seconds, on success) or a standby wait (`StandbyDuration` seconds, on a miss), the expected time between two random sessions is:

```
secondsBetweenSessions ≈ ((1 - SamplingRate) / SamplingRate) * StandbyDuration + Duration
sessionsPerHour ≈ 3600 / secondsBetweenSessions
```

Example: `SamplingRate = 0.05`, `Duration = 30s`, `StandbyDuration = 120s` (default) →
`((1 - 0.05) / 0.05) * 120 + 30 = 2310s` between sessions → ≈ **1.6 sessions/hour**.

Raising `SamplingRate` increases frequency (e.g., `0.1` → ≈ 3.1 sessions/hour). `StandbyDuration` defaults to 120s and is tuned server-side, not through the `ServiceProfiler` config section.

## Safety Checklist

- Production: Keep `AllowsCrash = false`.
- Do not use `SkipEndpointCertificateValidation` — it is deprecated and no longer functional.
- Review storage footprint if `PreserveTraceFile = true`.
- Validate custom providers in staging before production rollout.

## Troubleshooting Quick Reference

| Symptom | Suggested Adjustment |
|---------|----------------------|
| Few or no profiles | Lower thresholds, slightly raise SamplingRate |
| High overhead | Shorter Duration, reduce CustomEventPipeProviders |
| Large disk usage | Disable PreserveTraceFile, tighten scavenger options |
| Missing triggers | Verify thresholds below observed steady-state levels. See [CPU](CpuUsageMonitoring.md) and [Memory](MemoryUsageMonitoring.md) monitoring docs for diagnostic logging. |

## Versioning / Evolution

Defaults may evolve; pin critical behavior via explicit settings to avoid surprises during upgrades.
