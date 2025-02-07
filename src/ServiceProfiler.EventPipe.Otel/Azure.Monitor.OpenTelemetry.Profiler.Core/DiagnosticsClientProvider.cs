using System.Collections.Concurrent;
using Microsoft.Diagnostics.NETCore.Client;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal sealed class DiagnosticsClientProvider
{
    private readonly ConcurrentDictionary<int, DiagnosticsClient> _clients = new();
    private DiagnosticsClientProvider()
    {
    }
    
    public static DiagnosticsClientProvider Instance = new();

    public DiagnosticsClient GetDiagnosticsClient(int processId) => _clients.GetOrAdd(
        processId,
        static processId => new DiagnosticsClient(processId));
}