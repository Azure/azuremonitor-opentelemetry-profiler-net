using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;
using Microsoft.ServiceProfiler.Orchestration;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface ICustomEventsBuilder
{
    ServiceProfilerIndex CreateServiceProfilerIndex(string fileId, string stampId, DateTimeOffset sessionId, Guid appId, IProfilerSource profilerSource);

    IEnumerable<ServiceProfilerSample> CreateServiceProfilerSamples(IReadOnlyCollection<SampleActivity> samples, string stampId, DateTimeOffset sessionId, Guid appId);
}