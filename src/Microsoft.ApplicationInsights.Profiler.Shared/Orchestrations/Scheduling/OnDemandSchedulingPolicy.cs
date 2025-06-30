//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal sealed class OnDemandSchedulingPolicy : EventPipeSchedulingPolicy
{
    private readonly IProfilerSettingsService _profilerSettingsService;
    private string? _evaluatedCollectionPlan;
    private const int DefaultProfileNowDurationInSeconds = 120;

    /// <summary>
    /// Scheduling policy that will start profiling when the Azure Portal "Start Profiling" button is clicked
    /// </summary>
    public OnDemandSchedulingPolicy(
        IOptions<UserConfigurationBase> userConfiguration,
        ProfilerSettings profilerSettings,
        ProcessExpirationPolicy expirationPolicy,
        IProfilerSettingsService profilerSettingsService,
        IDelaySource delaySource,
        IResourceUsageSource resourceUsageSource,
        ILogger<OnDemandSchedulingPolicy> logger
    ) : base(
        userConfiguration.Value.Duration,
        TimeSpan.Zero,
        userConfiguration.Value.ConfigurationUpdateFrequency,
        profilerSettings,
        delaySource,
        expirationPolicy,
        resourceUsageSource,
        logger
    )
    {
        _profilerSettingsService = profilerSettingsService ?? throw new ArgumentNullException(nameof(profilerSettingsService));
    }

    public override string Source => nameof(OnDemandSchedulingPolicy);

    public override async Task<IEnumerable<(TimeSpan duration, ProfilerAction action)>> GetScheduleAsync()
    {
        Logger.LogTrace("Start to wait for profiler settings service to be ready.");

        // This is best effort.
        await _profilerSettingsService.WaitForInitializedAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        Logger.LogTrace("Profiler settings service is ready.");

        // A new collection plan is in place.
        if (!string.IsNullOrEmpty(ProfilerSettings.CollectionPlan) &&
            !string.Equals(ProfilerSettings.CollectionPlan, _evaluatedCollectionPlan, StringComparison.Ordinal))
        {
            _evaluatedCollectionPlan = ProfilerSettings.CollectionPlan;

            if (DateTime.Compare(DateTime.FromBinary(ProfilerSettings.Engine.Expiration).ToUniversalTime(), DateTime.UtcNow) >= 0)
            {
                int durationInSeconds = ProfilerSettings.Engine.ImmediateOptions.ProfilingDurationInSeconds;
                if (durationInSeconds <= 0)
                {
                    durationInSeconds = (int)ProfilingDuration.TotalSeconds;
                }

                // Client Safety. Notice, this shall not replace any server side check.
                if (durationInSeconds <= 0 || durationInSeconds > 360)
                {
                    Logger.LogWarning("Invalid Profile Now duration ({0}s) detected. Use the default value of {1}s instead.", durationInSeconds, DefaultProfileNowDurationInSeconds);
                    durationInSeconds = DefaultProfileNowDurationInSeconds;
                }

                return CreateProfilingSchedule(TimeSpan.FromSeconds(durationInSeconds));
            }
        }

        return CreateStandbySchedule();
    }
}
