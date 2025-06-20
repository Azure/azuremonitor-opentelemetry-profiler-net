//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Orchestration.MetricsProviders;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;

internal sealed class MemInfoFileMemoryMetricsProvider : IMetricsProvider
{
    private readonly ILogger _logger;
    private readonly MemInfoItemParser _memInfoItemParser;
    private readonly IMemInfoReader _memInfoReader;

    public MemInfoFileMemoryMetricsProvider(
        MemInfoItemParser memInfoItemParser,
        IMemInfoReader memInfoReader,
        ILogger<MemInfoFileMemoryMetricsProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _memInfoItemParser = memInfoItemParser ?? throw new ArgumentNullException(nameof(memInfoItemParser));
        _memInfoReader = memInfoReader ?? throw new ArgumentNullException(nameof(memInfoReader));
    }

    /// <summary>
    /// Get the memory usage in percentage.
    /// </summary>
    /// <returns>Returns the memory usages in form of 100 x percentage. For example, 25.5 for 25.5%.</returns>
    public float GetNextValue()
    {
        (float total, float free) = GetMetrics();

        _logger.LogDebug("Get memory usage: free/total: {free}/{total}", free, total);
        // Get the memory used, avoid div by 0.
        if (total != 0)
        {
            return (1 - free / total) * 100;
        }

        return 0;
    }

    internal (float total, float available) GetMetrics()
    {
        (string name, ulong value, string? unit, bool fetched) total = ("MemTotal", 0, null, false);
        (string name, ulong value, string? unit, bool fetched) available = ("MemAvailable", 0, null, false);

        using Stream stream = _memInfoReader.Read();

        if (stream is null)
        {
            _logger.LogDebug("No stream for data.");
            return (0, 0);
        }

        // Stream is not null
        using StreamReader reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            _logger.LogDebug("Read line: {line}", line);

            if (_memInfoItemParser.TryParse(line, out var metric))
            {
                if (string.Equals(metric.name, total.name, StringComparison.OrdinalIgnoreCase))
                {
                    total.value = metric.value;
                    total.unit = metric.unit;
                    total.fetched = true;
                }
                else if (string.Equals(metric.name, available.name, StringComparison.OrdinalIgnoreCase))
                {
                    available.value = metric.value;
                    available.unit = metric.unit;
                    available.fetched = true;
                }

                // Found all needed, break the loop.
                if (total.fetched && available.fetched && string.Equals(available.unit, total.unit, StringComparison.OrdinalIgnoreCase))
                {
                    return (total.value, available.value);
                }
            }
            else
            {
                _logger.LogDebug("Line parse failure for mem info: {line}. Move on to the next line", line);
            }
        }

        _logger.LogDebug("Reach the end of the meminfo file. Didn't find expected metrics.");
        return (0, 0);
    }
}
