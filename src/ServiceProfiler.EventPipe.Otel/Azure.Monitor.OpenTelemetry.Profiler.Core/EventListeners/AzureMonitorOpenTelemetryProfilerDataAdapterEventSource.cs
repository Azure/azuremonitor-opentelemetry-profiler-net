using System.Diagnostics.Tracing;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;

[EventSource(Name = EventSourceName)]
internal class AzureMonitorOpenTelemetryProfilerDataAdapterEventSource : EventSource
{
    public class Keywords
    {
        public const EventKeywords Operations = (EventKeywords)0x1;
    }

    public const string EventSourceName = "AzureMonitor-OpenTelemetry-Profiler-DataAdapter";

    internal static class EventId
    {
        public const int RequestStart = 1;
        public const int RequestStop = 2;
    }

#pragma warning disable CA2211 // Non-constant fields should not be visible
    public static AzureMonitorOpenTelemetryProfilerDataAdapterEventSource Log = new();
#pragma warning restore CA2211 // Non-constant fields should not be visible

    [Event(EventId.RequestStart, Keywords = Keywords.Operations)]
    public void RequestStart(string name, string id, string requestId, string operationId)
    {
        WriteEvent(EventId.RequestStart, name, id, requestId, operationId);
    }

    [Event(EventId.RequestStop, Keywords = Keywords.Operations)]
    public void RequestStop(string name, string id, string requestId, string operationId)
    {
        WriteEvent(EventId.RequestStop, name, id, requestId, operationId);
    }
}