//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

// TODO: Consider moving this to the the process monitoring project.

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.DataContract.Settings;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Orchestration.MetricsProviders;
using Microsoft.ServiceProfiler.ProcessMonitor;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal sealed class ResourceUsageSource : IResourceUsageSource
{
    private float _currentCPUBaseline = 0f;
    private float _currentMemoryBaseline = 0f;
    private bool _disposed = false;
    
    private readonly BaselineTracker? _cpuBaselineTracker;
    private readonly BaselineTracker? _memoryBaselineTracker;
    private readonly UserConfigurationBase _userConfigurations;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;

    ///<summary>
    /// Aggregates CPU and RAM usage in recent times.
    ///</summary>
    public ResourceUsageSource(
        [FromKeyedServices(MetricsProviderCategory.CPU)]
        IMetricsProvider cpuMetricsProvider,
        [FromKeyedServices(MetricsProviderCategory.Memory)]
        IMetricsProvider memoryMetricsProvider,
        CpuTriggerSettings cpuTriggerSettings,
        MemoryTriggerSettings memoryTriggerSettings,
        ISerializationProvider serializer,
        IOptions<UserConfigurationBase> userConfigurations,
        ILogger<ResourceUsageSource> logger,
        ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        _userConfigurations = userConfigurations?.Value ?? throw new ArgumentNullException(nameof(userConfigurations));

        if (cpuMetricsProvider is null)
        {
            throw new ArgumentNullException(nameof(cpuMetricsProvider));
        }

        if (memoryMetricsProvider is null)
        {
            throw new ArgumentNullException(nameof(memoryMetricsProvider));
        }

        if (serializer is null)
        {
            throw new ArgumentNullException(nameof(serializer));
        }

        if (cpuTriggerSettings is null)
        {
            throw new ArgumentNullException(nameof(cpuTriggerSettings));
        }
        else
        {
            if (serializer.TrySerialize(cpuTriggerSettings, out string? serializedCpuTriggerSettings))
            {
                _logger.LogDebug("CPU Triggering settings: {settings}", serializedCpuTriggerSettings);
            }
        }

        if (memoryTriggerSettings is null)
        {
            throw new ArgumentNullException(nameof(memoryTriggerSettings));
        }
        else
        {
            if (serializer.TrySerialize(memoryTriggerSettings, out string? serializedMemoryTriggerSettings))
            {
                _logger.LogDebug("Memory Triggering settings: {settings}", serializedMemoryTriggerSettings);
            }
        }

        if (_userConfigurations.IsDisabled)
        {
            _logger.LogDebug("Resource usage monitoring is disabled because profiler is disabled.");
            return;
        }

        _cpuBaselineTracker = CreateAndStartCPUBaselineTracker(cpuTriggerSettings, cpuMetricsProvider);
        _memoryBaselineTracker = CreateAndStartMemoryBaselineTracker(memoryMetricsProvider, memoryTriggerSettings);
    }

    public float GetAverageCPUUsage()
    => LogAndReturn(() => _currentCPUBaseline, MetricsProviderCategory.CPU);

    public float GetAverageMemoryUsage()
        => LogAndReturn(() => _currentMemoryBaseline, MetricsProviderCategory.Memory);

    private BaselineTracker CreateAndStartMemoryBaselineTracker(IMetricsProvider memoryMetricsProvider, MemoryTriggerSettings memoryTriggerSettings)
    {
        // TODO: Fix with ActivatorUtilities
        BaselineTracker memoryBaselineTracker = new(
            new RollingHistoryArray<float>(
                TimeSpan.FromMinutes(memoryTriggerSettings.MemoryRollingHistorySize),
                TimeSpan.FromSeconds(memoryTriggerSettings.MemoryRollingHistoryInterval)),
                TimeSpan.FromSeconds(memoryTriggerSettings.MemoryAverageWindow), memoryMetricsProvider.GetNextValue, _loggerFactory.CreateLogger<BaselineTracker>());

        memoryBaselineTracker.RegisterCallback((oldBaseline, newBaseline) =>
        {
            _logger.LogTrace("Memory monitoring calling back. Old base: {old}, new base: {new}", oldBaseline, newBaseline);
            _currentMemoryBaseline = newBaseline;
        });

        memoryBaselineTracker.Start();

        return memoryBaselineTracker;
    }

    private BaselineTracker CreateAndStartCPUBaselineTracker(CpuTriggerSettings cpuTriggerSettings, IMetricsProvider cpuMetricsProvider)
    {
        BaselineTracker cpuBaselineTracker = new(
            new RollingHistoryArray<float>(
                TimeSpan.FromMinutes(cpuTriggerSettings.CpuRollingHistorySize),
                TimeSpan.FromSeconds(cpuTriggerSettings.CpuRollingHistoryInterval)),
            TimeSpan.FromSeconds(cpuTriggerSettings.CpuAverageWindow), 
            getNextMetric: cpuMetricsProvider.GetNextValue,
            logger: _loggerFactory.CreateLogger<BaselineTracker>());

        cpuBaselineTracker.RegisterCallback((oldBaseline, newBaseline) =>
        {
            _logger.LogTrace("CPU monitoring calling back. Old base: {old}, new base: {new}", oldBaseline, newBaseline);
            _currentCPUBaseline = newBaseline;
        });

        cpuBaselineTracker.Start();
        return cpuBaselineTracker;
    }

    private float LogAndReturn(Func<float> valueProvider, MetricsProviderCategory category)
    {
        float value = valueProvider();
        _logger.LogDebug("Getting current {category} usage: {value}", category, value);
        return value;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cpuBaselineTracker?.Dispose();
            _memoryBaselineTracker?.Dispose();
            _disposed = true;
        }
    }
}
