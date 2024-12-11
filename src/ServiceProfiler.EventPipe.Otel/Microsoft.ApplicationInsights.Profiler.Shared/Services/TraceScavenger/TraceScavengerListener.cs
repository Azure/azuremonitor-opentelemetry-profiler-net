//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Utilities;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.TraceScavenger;

internal class TraceScavengerListener : IFileScavengerEventListener
{
    private readonly ILogger _logger;

    public TraceScavengerListener(ILogger<TraceScavengerListener> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void OnDeleteException(string path, Exception exception)
        => _logger.LogError(exception, "Failed deleting file: {path}", path);

    public void OnDeleteSuccess(string path)
        => _logger.LogDebug("Success processed file: {path}", path);

    public void OnFilesDiscovered(ICollection<string> filesToDelete)
        => _logger.LogTrace("Files discovered: {files}", filesToDelete);

    public void OnMessage(string messageFormat, params object[] args)
        => _logger.LogTrace("Message details: {details}", string.Format(CultureInfo.InvariantCulture, messageFormat, args));

    public void OnRunCancelled()
        => _logger.LogInformation("Run cancelled");

    public void OnRunFailed(Exception exception)
        => _logger.LogError(exception, "Fun failed.");

    public void OnRunSucceeded()
        => _logger.LogDebug("Run succeeded.");

    public void OnStarted()
        => _logger.LogInformation("File scavenger started.");
}

