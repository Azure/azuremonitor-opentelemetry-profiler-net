//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;

/// <summary>
/// A line parser to parse /proc/meminfo for Linux.
/// </summary>
internal sealed class MemInfoItemParser
{
    public bool TryParse(string line, out (string name, ulong value, string? unit) metric)
    {
        string[] tokens = line.Split([':'], StringSplitOptions.RemoveEmptyEntries);
        if (tokens?.Length == 2)
        {
            string name = tokens[0].Trim();
            bool result = TryParseMemInfoValue(tokens[1], out ulong value, out string? unit);
            metric = (name, value, unit);
            return result;
        }
        else
        {
            metric = default;
            return false;
        }
    }

    internal bool TryParseMemInfoValue(string input, out ulong value, out string? unit)
    {
        input = input.Trim();
        value = default;
        unit = default;
        string[] tokens = input.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (tokens?.Length == 2)
        {
            if (ulong.TryParse(tokens[0], out value))
            {
                unit = tokens[1].Trim();
                return true;
            }
        }

        return false;
    }
}