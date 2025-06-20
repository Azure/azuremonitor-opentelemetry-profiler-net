using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;

internal record ServiceProfilerIndex
{
    public DateTime Timestamp { get; init; }
    public string FileId { get; init; } = null!;
    public string StampId { get; init; } = null!;
    public string DataCube { get; init; } = null!;
    public string EtlFileSessionId { get; init; } = null!;
    public string MachineName { get; init; } = null!;
    public int ProcessId { get; init; }


    public string Source { get; init; } = null!;

    public string OperatingSystem { get; init; } = null!;

    public double AverageCPUUsage { get; init; }
    public double AverageMemoryUsage { get; init; }

    public string CloudRoleName { get; init; } = null!;
}