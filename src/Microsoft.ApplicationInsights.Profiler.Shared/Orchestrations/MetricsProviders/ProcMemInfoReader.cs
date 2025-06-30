//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.IO;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;

/// <summary>
/// An implementation to get meminfo from proc/meminfo file.
/// </summary>
internal class ProcMemInfoReader : IMemInfoReader
{
    private const string DefaultMemInfoPath = @"/proc/meminfo";
    private readonly ILogger<ProcMemInfoReader> _logger;

    public ProcMemInfoReader(ILogger<ProcMemInfoReader> logger)
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc>
    public Stream Read(string? filePath)
    {
        // Setup default file path for meminfo.
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = DefaultMemInfoPath;
        }

        _logger.LogDebug("Open read mem file: {filePath}", filePath);
        try
        {
            return File.OpenRead(filePath);
        }
        catch (IOException ex)
        {
            _logger.LogDebug(ex, "Failed open meminfo file.");
            return Stream.Null;
        }
    }
}
