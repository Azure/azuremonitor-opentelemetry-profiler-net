﻿//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal class SampleActivity
{
    public string? StartActivityIdPath { get; set; }
    public string? StopActivityIdPath { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset StopTimeUtc { get; set; }
    public string? RequestId { get; set; }
    public string? RoleInstance { get; set; }
    public string? OperationName { get; set; }
    public string? OperationId { get; set; }
    public TimeSpan Duration
    {
        get
        {
            if (_duration != null && _duration.HasValue)
            {
                return _duration.Value;
            }

            return StopTimeUtc - StartTimeUtc;
        }
        set
        {
            if (_duration == null || _duration.Value != value)
            {
                _duration = value;
            }
        }
    }

    public bool IsValid(ILogger logger)
    {
        // There must be start/stop activity id paths
        if (!IsGoodActivityIdPath(StartActivityIdPath, logger)) { return false; }
        if (!IsGoodActivityIdPath(StopActivityIdPath, logger)) { return false; }
        // Start/Stop activity id path should either be the same (.NET Core 2.x) or the start activity id starts with stop activity id path (.NET Core 3.0).
        // Notes: this is activity id path for events from Microsoft-ApplicationInsights-DataRelay.
        if (!StartActivityIdPath!.StartsWith(StopActivityIdPath, StringComparison.Ordinal)) { return false; }
        if (StartTimeUtc >= StopTimeUtc) { return false; }
        if (Duration.TotalMilliseconds <= 0) { return false; }
        if (string.IsNullOrEmpty(RequestId)) { return false; }

        return true;
    }

    private bool IsGoodActivityIdPath(string? activityId, ILogger logger)
    {
        if (string.IsNullOrEmpty(activityId))
        {
            logger?.LogTrace("Invalid activity id path. Activity id is empty.");
            return false;
        }

        // TODO: pay attention to the performance of the regular expression. Refactor it when time permits.
        // Exclude activity id in form of: /#1503500717/
        Regex regex = _validActivityIdPathRegex;
        if (regex.IsMatch(activityId))
        {
            logger?.LogTrace("Activity id {0} doesn't match regex: {1}", activityId, regex);
            return false;
        }

        // Passed all validation
        return true;
    }

    private TimeSpan? _duration = null;
    private static readonly Regex _validActivityIdPathRegex = new(@"^/#[\d]*?/$", RegexOptions.Compiled, TimeSpan.FromSeconds(1));
}