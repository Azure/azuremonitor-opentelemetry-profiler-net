// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Diagnostics.NETCore.Client;
using ServiceProfiler.Common.Utilities;

namespace Microsoft.ApplicationInsights.Profiler.Core.TraceControls;

internal sealed class DiagnosticsClientProvider
{
    private readonly ConcurrentDictionary<int, DiagnosticsClient> _clients = new();
    private DiagnosticsClientProvider()
    {
    }
    public static DiagnosticsClientProvider Instance = new();

    public DiagnosticsClient GetDiagnosticsClient() => _clients.GetOrAdd(
        CurrentProcessUtilities.GetId(),
        static processId => new DiagnosticsClient(processId));
}
