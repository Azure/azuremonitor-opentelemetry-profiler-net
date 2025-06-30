//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text.Json;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal abstract class DiagnosticsClientTraceConfigurationBase
{
    private readonly UserConfigurationBase _userConfiguration;
    private readonly ILogger _logger;

    /// <summary>
    /// Gets the settings for the circular buffer.
    /// </summary>
    public int CircularBufferMB { get; }

    /// <summary>
    /// Gets whether to request rundown or not. For profiler, this should almost always be true.
    /// </summary>
    public bool RequestRundown { get; }

    private readonly IReadOnlyCollection<EventPipeProvider> _eventPipeProvidersCache = [];

    public DiagnosticsClientTraceConfigurationBase(IOptions<UserConfigurationBase> userConfiguration, ILogger<DiagnosticsClientTraceConfigurationBase> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userConfiguration = userConfiguration?.Value ?? throw new ArgumentNullException(nameof(userConfiguration));

        RequestRundown = true;
        CircularBufferMB = _userConfiguration.BufferSizeInMB;
    }

    /// <summary>
    /// Build a collection of event pipe profilers. It includes all pre-defined providers to make the profiler work,
    /// and any other providers by user options.
    /// </summary>
    public IReadOnlyCollection<EventPipeProvider> BuildEventPipeProviders()
    {
        // Avoid repeat computing.
        if (_eventPipeProvidersCache.Any())
        {
            return _eventPipeProvidersCache;
        }

        IReadOnlyCollection<EventPipeProvider>? result = null;

        // Built in providers with provider name as key, case sensitive.
        Dictionary<string, EventPipeProvider> providerHolder = BuildPredefinedServiceProfilerProviders().ToDictionary(item => item.Name, StringComparer.Ordinal);

        // Appending custom providers when there's any.
        EventPipeProviderItem[]? customProviders = _userConfiguration.CustomEventPipeProviders?.ToArray();
        try
        {
            if (customProviders is not null && customProviders.Length > 0)
            {
                _logger.LogDebug("There's custom providers configured. Count: {customProviderCount}", customProviders.Length);
                int newProviderCount = 0;
                int replacedProviderCount = 0;
                foreach (EventPipeProviderItem providerInfo in customProviders)
                {
                    if (providerHolder.ContainsKey(providerInfo.Name))
                    {
                        // Replace existing provider
                        _logger.LogTrace("Replacing existing provider: {providerName}", providerInfo.Name);
                        providerHolder[providerInfo.Name] = providerInfo.ToProvider();
                        replacedProviderCount++;
                    }
                    else
                    {
                        // Append new provider
                        _logger.LogTrace("Appending new provider: {providerName}", providerInfo.Name);
                        providerHolder.Add(providerInfo.Name, providerInfo.ToProvider());
                        newProviderCount++;
                    }
                }

                _logger.LogInformation("Custom providers configured. New: {newCount}, replaced: {replacedCount}, total: {totalProviderCount}.", newProviderCount, replacedProviderCount, providerHolder.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception configuring custom EventPipe providers. Please refer to https://aka.ms/ep-sp/custom-providers for instructions.");
            _logger.LogTrace(ex, "Full Error: {fullError}", ex.ToString());
        }
        finally
        {
            result = [.. providerHolder.Values];
        }

        // Potentially heavy serialization, run it only when debugging / tracing.
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("EventPipe provider list:");
            foreach (EventPipeProvider provider in result)
            {
                _logger.LogInformation("{provider}", JsonSerializer.Serialize(provider));
            }
        }

        return result ?? [];
    }

    private IReadOnlyCollection<EventPipeProvider> BuildPredefinedServiceProfilerProviders()
    {
        List<EventPipeProvider> providerList =
        [
            // Provider Name: Microsoft-Windows-DotNETRuntime("e13c0d23-ccbc-4e12-931b-d9cc2eee27e4")
            // This is the CLR provider.  The most important of these are the GC events, JIT compilation events.
            // (the JIT events are needed to decode the stack addresses).
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Verbose, keywords: 0x4c14fccbd, arguments: null),

            // Private provider.
            // Provider Name: Microsoft-Windows-DotNETRuntimePrivate("763fd754-7086-4dfe-95eb-c01a46faf4ca")
            // This is the 'Private' CLR provider. We would like to get rid of this, and mostly only has some less important GC events
            new EventPipeProvider("Microsoft-Windows-DotNETRuntimePrivate", EventLevel.Verbose, keywords: 0x4002000b, arguments: null),

            // Sample profiler.
            // Profiler Name: Microsoft-DotNETCore-SampleProfiler("3c530d44-97ae-513a-1e6d-783e8f8e03a9")
            // This is the provider that generates a CPU stack every msec.
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose, keywords: 0x0, arguments: null),

            // TPL.
            // Provider Name: System.Threading.Tasks.TplEventSource("2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5")
            // This is the provider that generates ‘Task Parallel Library’ events.
            // These events are needed to stitch together stacks from different threads (when async I/O happens).
            new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Verbose, keywords: 0x1 | 0x2 | 0x4 | 0x40 | 0x80, arguments: null),
        ];

        providerList.AddRange(AppendEventPipeProviders());

        return providerList.AsReadOnly();
    }

    /// <summary>
    /// Appending specific EventSource providers by different client according to the needs of the SDKs.
    /// </summary>
    protected abstract IEnumerable<EventPipeProvider> AppendEventPipeProviders();
}
