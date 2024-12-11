using System;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

public abstract class EventPipeSchedulingPolicy : SchedulingPolicy
{
    public EventPipeSchedulingPolicy(
        TimeSpan profilingDuration,
        TimeSpan profilingCooldown,
        TimeSpan pollingInterval,
        ProfilerSettings profilerSettings,
        IDelaySource delaySource,
        IExpirationPolicy expirationPolicy,
        IResourceUsageSource resourceUsageSource,
        ILogger<EventPipeSchedulingPolicy> logger
    ) : base(profilingDuration, profilingCooldown, pollingInterval, delaySource, expirationPolicy, logger)
    {
        ProfilerSettings = profilerSettings;
        ResourceUsageSource = resourceUsageSource;
    }

    protected T UpdateRefreshAndGetSetting<T>(T newSetting, T currentSetting, ref bool needsRefresh)
    {
        needsRefresh = needsRefresh || currentSetting is not null && !currentSetting.Equals(newSetting);

        return newSetting;
    }
    protected override bool PolicyNeedsRefresh()
    {
        if (ProfilerSettings.Enabled != ProfilerEnabled)
        {
            ProfilerEnabled = ProfilerSettings.Enabled;
            return true;
        }

        return false;
    }

    protected ProfilerSettings ProfilerSettings { get; }
    protected IResourceUsageSource ResourceUsageSource { get; }

    protected IEnumerable<(TimeSpan duration, ProfilerAction action)> CreateProfilingSchedule(TimeSpan profilingDuration)
    {
        yield return (profilingDuration, ProfilerAction.StartProfilingSession);
        yield return (ProfilingCooldown, ProfilerAction.Standby);
    }

    protected IEnumerable<(TimeSpan duration, ProfilerAction action)> CreateStandbySchedule()
    {
        yield return (PollingInterval, ProfilerAction.Standby);
    }
}