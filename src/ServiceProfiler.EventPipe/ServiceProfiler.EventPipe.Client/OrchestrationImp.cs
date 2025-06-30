using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Core;

internal class OrchestrationImp : OrchestratorEventPipe
{
    public OrchestrationImp(
        IServiceProfilerProvider profilerProvider,
        IOptions<UserConfiguration> config, 
        IEnumerable<SchedulingPolicy> policyCollection, 
        IDelaySource delaySource, ILogger<OrchestratorEventPipe> logger) : base(profilerProvider, config, policyCollection, delaySource, logger)
    {
    }
}
