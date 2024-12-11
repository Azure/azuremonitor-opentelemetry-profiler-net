//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal class OneTimeSchedulingPolicy : EventPipeSchedulingPolicy
{
    public OneTimeSchedulingPolicy(
        IOptions<UserConfigurationBase> userConfiguration,
        ProfilerSettings profilerSettings,
        IDelaySource delaySource,
        IResourceUsageSource resourceUsageSource,
        LimitedExpirationPolicyFactory limitedExpirationPolicyFactory,
        ILogger<OneTimeSchedulingPolicy> logger
    ) : base(
        userConfiguration.Value.Duration,
        TimeSpan.FromHours(4),
        userConfiguration.Value.ConfigurationUpdateFrequency,
        profilerSettings,
        delaySource,
        limitedExpirationPolicyFactory.Create(1),
        resourceUsageSource,
        logger
    )
    { }

    public override string Source { get; } = nameof(OneTimeSchedulingPolicy);
    public override Task<IEnumerable<(TimeSpan duration, ProfilerAction action)>> GetScheduleAsync()
    {
        return Task.FromResult(new List<(TimeSpan, ProfilerAction)>()
            {
                (ProfilingDuration, ProfilerAction.StartProfilingSession),
                (ProfilingDuration, ProfilerAction.Standby), // The duration will be overwritten by the expiration policy.
            }.AsEnumerable());
    }

    protected override bool PolicyNeedsRefresh() => false;
}

