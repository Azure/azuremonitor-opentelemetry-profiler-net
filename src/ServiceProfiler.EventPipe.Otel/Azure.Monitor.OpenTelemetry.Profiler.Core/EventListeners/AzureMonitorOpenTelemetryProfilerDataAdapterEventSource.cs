using System.Diagnostics.Tracing;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;

[EventSource(Name = EventSourceName)]
public class AzureMonitorOpenTelemetryProfilerDataAdapterEventSource : EventSource
{
    public const string EventSourceName = "AzureMonitor-OpenTelemetry-Profiler-DataAdapter";

#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static AzureMonitorOpenTelemetryProfilerDataAdapterEventSource Log = new();
#pragma warning restore CA2211 // Non-constant fields should not be visible

    [Event(1)]
    public void RequestStart(string name, string id)
    {
        WriteEvent(1, name, id);
    }

    [Event(2)]
    public void RequestStop(string name, string id)
    {
        WriteEvent(2, name, id);
    }
}