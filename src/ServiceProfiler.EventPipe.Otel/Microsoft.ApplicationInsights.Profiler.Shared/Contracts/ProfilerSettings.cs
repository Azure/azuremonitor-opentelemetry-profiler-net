//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.ServiceProfiler.DataContract.Settings;
using Microsoft.ServiceProfiler.DataContract.Agent.Settings;
using Microsoft.ServiceProfiler.Orchestration.Modes;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

/// <summary>
/// Settings contract for the Event Pipe agent.
/// Contains the values provided in a user configuration on load and later from remote trigger settings if and when they are set.
/// </summary>
internal class ProfilerSettings
{
    private readonly SettingsParser _settingsParser;
    private readonly ILogger _logger;

    public ProfilerSettings(
        IOptions<UserConfigurationBase> userConfiguration,
        IProfilerSettingsService settingsService,
        SettingsParser settingsParser,
        ILogger<ProfilerSettings> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (userConfiguration is null)
        {
            throw new ArgumentNullException(nameof(userConfiguration));
        }

        if (settingsService is null)
        {
            throw new ArgumentNullException(nameof(settingsService));
        }
        _settingsParser = settingsParser ?? throw new ArgumentNullException(nameof(settingsParser));

        Enabled = !userConfiguration.Value.IsDisabled;
        SamplingOptions.SamplingRate = userConfiguration.Value.RandomProfilingOverhead;
        SamplingOptions.ProfilingDurationInSeconds = (int)userConfiguration.Value.Duration.TotalSeconds;
        CpuTriggerSettings.CpuThreshold = userConfiguration.Value.CPUTriggerThreshold;
        MemoryTriggerSettings.MemoryThreshold = userConfiguration.Value.MemoryTriggerThreshold;
        settingsService.SettingsUpdated += SetFromSettingsContract;
    }

    private void SetFromSettingsContract(SettingsContract settingsContract)
    {
        if (settingsContract is null)
        {
            throw new ArgumentNullException(nameof(settingsContract));
        }

        string[] settingsToParse = {
                settingsContract.CollectionPlan,
                settingsContract.CpuTriggerConfiguration,
                settingsContract.MemoryTriggerConfiguration,
                settingsContract.DefaultConfiguration
            };

        AgentSettings parsedSettings = _settingsParser.ParseManyAgentSettings(settingsToParse.Where(setting => !String.IsNullOrEmpty(setting)).ToArray());

        if (parsedSettings is not null)
        {
            parsedSettings.Enabled = settingsContract.Enabled;
            Enabled = parsedSettings.Enabled;
            SamplingOptions = parsedSettings.Engine.SamplingOptions ?? SamplingOptions;
            CpuTriggerSettings = parsedSettings.Engine.CpuTriggerSettings ?? CpuTriggerSettings;
            MemoryTriggerSettings = parsedSettings.Engine.MemoryTriggerSettings ?? MemoryTriggerSettings;
            CollectionPlan = settingsContract.CollectionPlan ?? CollectionPlan;
            Engine = parsedSettings.Engine ?? Engine;
        }
        else
        {
            _logger.LogWarning("No remote settings have been configured for this IKey.");
        }
    }

    public bool Enabled
    {
        get;
        private set;
    } = true;

    public string? CollectionPlan
    {
        get;
        private set;
    }

    /// <summary>
    /// Settings for the agent's profiler engine.
    /// </summary>
    public EngineSettings Engine
    {
        get;
        set;
    } = new EngineSettings();

    public SamplingOptions SamplingOptions
    {
        get;
        private set;
    } = new SamplingOptions();

    public CpuTriggerSettings CpuTriggerSettings
    {
        get;
        private set;
    } = new CpuTriggerSettings();

    public MemoryTriggerSettings MemoryTriggerSettings
    {
        get;
        private set;
    } = new MemoryTriggerSettings();
}