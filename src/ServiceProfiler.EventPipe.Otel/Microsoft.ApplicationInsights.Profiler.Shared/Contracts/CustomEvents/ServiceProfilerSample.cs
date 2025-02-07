using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;

internal record ServiceProfilerSample
{
    public DateTime Timestamp { get; init; }
    public string RequestId { get; init; } = null!;
    public string ServiceProfilerContent { get; init; } = null!;
    public string ServiceProfilerVersion { get; init; } = "v2";

    public string? OperationName { get; init; }
    public string? OperationId { get; init; }

    public string? RoleName { get; init; }
    public string? RoleInstance { get; init; }
}