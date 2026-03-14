//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Orchestration.Modes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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
        IAgentStatusService agentStatusService,
        ILogger<RandomSchedulingPolicy> logger)
        : base(
            profilingDuration: TimeSpan.FromSeconds(profilerSettings.SamplingOptions.ProfilingDurationInSeconds),
            profilingCooldown: TimeSpan.Zero,
            pollingInterval: userConfiguration.Value.ConfigurationUpdateFrequency,
            profilerSettings: profilerSettings,
            delaySource,
            expirationPolicy,
            resourceUsageSource,
            agentStatusService,
            logger
        )
    {
        PolicyEnabled = profilerSettings.SamplingOptions.Enabled;
        _randomSource = randomSource ?? throw new ArgumentNullException(nameof(randomSource));
        _overhead = profilerSettings.SamplingOptions.SamplingRate;
    }

    public override IAsyncEnumerable<(TimeSpan duration, ProfilerAction action)> GetScheduleAsync(CancellationToken cancellationToken)
    {
        var result = new List<(TimeSpan duration, ProfilerAction action)>();

        // Clamp overhead to valid range to prevent infinite loop when targetCount > segments.
        double clampedOverhead = Math.Clamp(_overhead, 0, 1);
        if (clampedOverhead != _overhead)
        {
            Logger.LogWarning("SamplingRate {overhead} is outside the valid range [0, 1]. Clamping to {clampedOverhead}.", _overhead, clampedOverhead);
        }

        if (ProfilingDuration.TotalSeconds <= 0)
        {
            Logger.LogWarning("ProfilingDuration {duration} is not positive. Falling back to standby.", ProfilingDuration);
            result.Add((PollingInterval, ProfilerAction.Standby));
            return result.ToAsyncEnumerable();
        }

        // Given this interval these are the number of possible segments for profiling
        var segments = (int)Math.Round(_scheduleInterval.TotalSeconds / ProfilingDuration.TotalSeconds);
        if (segments == 0)
        {
            throw new InvalidOperationException("No valid segment for random scheduling.");
        }

        var targetCount = (int)Math.Round(_scheduleInterval.TotalSeconds * clampedOverhead / ProfilingDuration.TotalSeconds);
        // Safety clamp: targetCount must never exceed segments to prevent an infinite picking loop.
        targetCount = Math.Min(targetCount, segments);

        Logger.LogDebug("Overhead is set to {overhead:p}. {count} profiling sessions expected over the period of {totalRunning}, each session will run for: {duration}. More periods will be scheduled in the future",
            clampedOverhead, targetCount, _scheduleInterval, ProfilingDuration);
        // No segments needed. This will happen when overhead is set to 0.
        if (targetCount == 0)
        {
            result.Add((PollingInterval, ProfilerAction.Standby));
            return result.ToAsyncEnumerable();
        }

        // Pick random segments according to how many random segments the user wants for this interval.
        // When targetCount is more than half of segments, pick segments to EXCLUDE instead (complement approach)
        // to avoid the coupon collector problem where random rejection sampling becomes very slow.
        var randomPicks = PickRandomSegments(targetCount, segments, cancellationToken);

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
        return result.ToAsyncEnumerable();
    }

    protected override bool PolicyNeedsRefresh()
    {
        bool generalPolicyNeedsRefresh = base.PolicyNeedsRefresh();

        bool needsRefresh = false;
        SamplingOptions samplingSettings = ProfilerSettings.SamplingOptions;

        PolicyEnabled = UpdateRefreshAndGetSetting(samplingSettings.Enabled, PolicyEnabled, ref needsRefresh);
        ProfilingDuration = UpdateRefreshAndGetSetting(TimeSpan.FromSeconds(samplingSettings.ProfilingDurationInSeconds), ProfilingDuration, ref needsRefresh);
        _overhead = UpdateRefreshAndGetSetting(samplingSettings.SamplingRate, _overhead, ref needsRefresh);

        // Either the base policy needs refresh or any of the random sampling settings changed.
        return generalPolicyNeedsRefresh || needsRefresh;
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

    /// <summary>
    /// Picks <paramref name="targetCount"/> distinct random segment indices from [0, <paramref name="segments"/>).
    /// Uses complement selection when targetCount > segments/2 to avoid the coupon collector problem.
    /// </summary>
    private HashSet<int> PickRandomSegments(int targetCount, int segments, CancellationToken cancellationToken)
    {
        if (targetCount <= segments / 2)
        {
            // Standard rejection sampling — efficient when picking a small fraction.
            var picks = new HashSet<int>();
            while (picks.Count < targetCount)
            {
                cancellationToken.ThrowIfCancellationRequested();
                picks.Add(NextSegmentIndex(segments));
            }
            return picks;
        }
        else
        {
            // Complement approach: pick (segments - targetCount) segments to EXCLUDE,
            // then return the complement. Much faster when targetCount is close to segments.
            int excludeCount = segments - targetCount;
            var excludes = new HashSet<int>();
            while (excludes.Count < excludeCount)
            {
                cancellationToken.ThrowIfCancellationRequested();
                excludes.Add(NextSegmentIndex(segments));
            }
            var picks = new HashSet<int>();
            for (int i = 0; i < segments; i++)
            {
                if (!excludes.Contains(i))
                {
                    picks.Add(i);
                }
            }
            return picks;
        }
    }

    /// <summary>
    /// Returns a uniform random index in [0, <paramref name="segments"/>).
    /// Uses NextDouble() to avoid modulo bias from Next() % segments.
    /// </summary>
    private int NextSegmentIndex(int segments) => (int)(_randomSource.NextDouble() * segments);
}
