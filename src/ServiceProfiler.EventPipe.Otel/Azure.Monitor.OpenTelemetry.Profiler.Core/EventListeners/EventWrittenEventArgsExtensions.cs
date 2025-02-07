using System.Diagnostics.Tracing;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;

internal static class EventWrittenEventArgsExtensions
{
    public static T? GetPayload<T>(this EventWrittenEventArgs eventData, string name)
    {
        if (eventData is null)
        {
            throw new InvalidOperationException("Event data is required.");
        }

        if (eventData.PayloadNames is null || !eventData.PayloadNames.Any())
        {
            return default;
        }

        int index = IndexAt(eventData.PayloadNames, name);
        if (index == -1)
        {
            return default;
        }

        if (eventData.Payload is null)
        {
            throw new InvalidDataException("Event data payload doesn't match names.");
        }

        object? result = eventData.Payload[index];
        if (result is null)
        {
            return default;
        }
        return (T)result;
    }

    private static int IndexAt(IEnumerable<string> payloadNames, string target)
    {
        int i = 0;
        foreach (string name in payloadNames)
        {
            if (string.Equals(name, target, StringComparison.Ordinal))
            {
                return i;
            }
            i++;
        }
        return -1;
    }
}