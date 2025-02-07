using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class OrchestrationImp : OrchestratorEventPipe
{
    public OrchestrationImp(IServiceProfilerProvider profilerProvider,
        IOptions<ServiceProfilerOptions> config,
        IEnumerable<SchedulingPolicy> policyCollection,
        IDelaySource delaySource,
        ILogger<OrchestratorEventPipe> logger)
        : base(profilerProvider, config, policyCollection, delaySource, logger)
    {
    }
}