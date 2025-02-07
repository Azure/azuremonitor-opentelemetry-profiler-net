using Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal record IPCAdditionalData
{
    public string? ConnectionString { get; set; }

    public ServiceProfilerIndex ServiceProfilerIndex { get; init; } = default!;

    public IEnumerable<ServiceProfilerSample> ServiceProfilerSamples { get; init; } = default!;
}