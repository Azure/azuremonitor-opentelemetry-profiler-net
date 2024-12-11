//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Orchestration.Modes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

/// <summary>
/// Scheduling policy that will profile on a schedule such that a certain percentage of the day consists of profiling, the rest is idling.
/// </summary>
internal sealed class RandomSchedulingPolicy : EventPipeSchedulingPolicy
{
    private readonly IRandomSource _randomSource;

    // Pre-calculate for 12 hours.
    private static readonly TimeSpan _scheduleInterval = TimeSpan.FromHours(12);
    private double _overhead;

    public RandomSchedulingPolicy(
        IOptions<UserConfigurationBase> userConfiguration,
        ProfilerSettings profilerSettings,
        ProcessExpirationPolicy expirationPolicy,
        IDelaySource delaySource,
        IRandomSource randomSource,
        IResourceUsageSource resourceUsageSource,
        ILogger<RandomSchedulingPolicy> logger)
        : base(
            profilingDuration: TimeSpan.FromSeconds(profilerSettings.SamplingOptions.ProfilingDurationInSeconds),
            profilingCooldown: TimeSpan.Zero,
            pollingInterval: userConfiguration.Value.ConfigurationUpdateFrequency,
            profilerSettings: profilerSettings,
            delaySource,
            expirationPolicy,
            resourceUsageSource,
            logger
        )
    {
        PolicyEnabled = profilerSettings.SamplingOptions.Enabled;
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        _overhead = profilerSettings.SamplingOptions.SamplingRate;
    }

    public override Task<IEnumerable<(TimeSpan duration, ProfilerAction action)>> GetScheduleAsync()
    {
        var result = new List<(TimeSpan duration, ProfilerAction action)>();

        // Given this interval these are the number of possible segments for profiling
        var targetCount = (int)Math.Round(_scheduleInterval.TotalSeconds * _overhead / ProfilingDuration.TotalSeconds);
        Logger.LogDebug("Overhead is set to {overhead:p}. {count} proifling sessions expected over the period of {totalRunning}, each session will run for: {duration}. More periods will be scheduled in the future",
            _overhead, targetCount, _scheduleInterval, ProfilingDuration);
        // No segments needed. This will happen when overhead is set to 0.
        if (targetCount == 0)
        {
            result.Add((PollingInterval, ProfilerAction.Standby));
            return Task.FromResult(result.AsEnumerable());
        }

        // Divide total duration into segments.
        var segments = (int)Math.Round(_scheduleInterval.TotalSeconds / ProfilingDuration.TotalSeconds);
        if (segments == 0)
        {
            throw new InvalidOperationException("No valid segment for random scheduling.");
        }

        // Pick random segments according to how many random segments the user wants for this interval
        var randomPicks = new HashSet<int>();
        while (randomPicks.Count < targetCount)
        {
            var pick = _randomSource.Next() % segments;
            if (!randomPicks.Contains(pick))
            {
                randomPicks.Add(pick);
            }
        }

        // Figure out how long we should stand by between the randomly scheduling profiling events to fill in the interval
        int accumulatedStandbySegments = 0;
        foreach (var segment in Enumerable.Range(0, segments))
        {
            if (randomPicks.Contains(segment))
            {
                // Flush accumulated standby segments
                if (accumulatedStandbySegments > 0)
                {
                    TimeSpan newStandby = TimeSpan.FromSeconds(ProfilingDuration.TotalSeconds * accumulatedStandbySegments);
                    MergeStandbyDuration(result, newStandby);
                    accumulatedStandbySegments = 0;
                }

                // Invoke start profiling
                result.Add((ProfilingDuration, ProfilerAction.StartProfilingSession));
                // Stop the profiling afterwards.
                result.Add((ProfilingCooldown, ProfilerAction.Standby));
            }
            else
            {
                accumulatedStandbySegments++;
            }
        }

        // Flush accumulated standby segments
        if (accumulatedStandbySegments > 0)
        {
            TimeSpan newStandby = TimeSpan.FromSeconds(ProfilingDuration.TotalSeconds * accumulatedStandbySegments);
            MergeStandbyDuration(result, newStandby);
        }

#if DEBUG
        int count = 0;
        foreach (var plan in result)
        {
            Logger.LogDebug("{0}.\tRandom plan - Duration: {1}, action: {2}", ++count, plan.Item1, plan.Item2);
        }
#endif
        return Task.FromResult(result.AsEnumerable());
    }

    protected override bool PolicyNeedsRefresh()
    {
        bool needsRefresh = false;
        SamplingOptions samplingSettings = ProfilerSettings.SamplingOptions;

        ProfilerEnabled = UpdateRefreshAndGetSetting(ProfilerSettings.Enabled, ProfilerEnabled, ref needsRefresh);
        PolicyEnabled = UpdateRefreshAndGetSetting(samplingSettings.Enabled, PolicyEnabled, ref needsRefresh);
        ProfilingDuration = UpdateRefreshAndGetSetting(TimeSpan.FromSeconds(samplingSettings.ProfilingDurationInSeconds), ProfilingDuration, ref needsRefresh);
        _overhead = UpdateRefreshAndGetSetting(samplingSettings.SamplingRate, _overhead, ref needsRefresh);

        return needsRefresh;
    }

    public override string Source { get; } = nameof(RandomSchedulingPolicy);

    private void MergeStandbyDuration(List<(TimeSpan duration, ProfilerAction action)> list, TimeSpan value)
    {
        int lastIndex = list.Count - 1;
        if (list.Count > 0 && list[lastIndex].action == ProfilerAction.Standby)
        {
            list[lastIndex] = (list[lastIndex].duration.Add(value), ProfilerAction.Standby);
        }
        else
        {
            list.Add((value, ProfilerAction.Standby));
        }
    }
}
