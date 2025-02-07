//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ServiceProfiler.DataContract.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal sealed class MemoryMonitoringSchedulingPolicy : EventPipeSchedulingPolicy
{
    private float _memoryThreshold;

    /// <summary>
    /// Scheduling policy that will start profiling when recent average RAM usage exceeds a threshold
    /// </summary>
    public MemoryMonitoringSchedulingPolicy(
        IOptions<UserConfigurationBase> userConfiguration,
        ProfilerSettings profilerSettings,
        ProcessExpirationPolicy expirationPolicy,
        IDelaySource delaySource,
        IResourceUsageSource resourceUsageSource,
        ILogger<MemoryMonitoringSchedulingPolicy> logger
    ) : base(
        userConfiguration.Value.Duration,
        userConfiguration.Value.MemoryTriggerCooldown,
        userConfiguration.Value.ConfigurationUpdateFrequency,
        profilerSettings,
        delaySource,
        expirationPolicy,
        resourceUsageSource,
        logger
    )
    {
        _memoryThreshold = profilerSettings.MemoryTriggerSettings.MemoryThreshold;
    }

    public override string Source => nameof(MemoryMonitoringSchedulingPolicy);

    public override Task<IEnumerable<(TimeSpan duration, ProfilerAction action)>> GetScheduleAsync()
    {
        float memoryUsage = ResourceUsageSource.GetAverageMemoryUsage();
        Logger.LogTrace("Memory Usage: {0}", memoryUsage);

        if (memoryUsage > _memoryThreshold)
        {
            return Task.FromResult(CreateProfilingSchedule(ProfilingDuration));
        }

        return Task.FromResult(CreateStandbySchedule());
    }

    protected override bool PolicyNeedsRefresh()
    {
        bool needsRefresh = false;
        MemoryTriggerSettings memorySettings = ProfilerSettings.MemoryTriggerSettings;

        ProfilerEnabled = UpdateRefreshAndGetSetting(ProfilerSettings.Enabled, ProfilerEnabled, ref needsRefresh);
        PolicyEnabled = UpdateRefreshAndGetSetting(memorySettings.Enabled, PolicyEnabled, ref needsRefresh);
        ProfilingDuration = UpdateRefreshAndGetSetting(TimeSpan.FromSeconds(memorySettings.MemoryTriggerProfilingDurationInSeconds), ProfilingDuration, ref needsRefresh);
        ProfilingCooldown = UpdateRefreshAndGetSetting(TimeSpan.FromSeconds(memorySettings.MemoryTriggerCooldownInSeconds), ProfilingCooldown, ref needsRefresh);
        _memoryThreshold = UpdateRefreshAndGetSetting(memorySettings.MemoryThreshold, _memoryThreshold, ref needsRefresh);

        return needsRefresh;
    }
}

