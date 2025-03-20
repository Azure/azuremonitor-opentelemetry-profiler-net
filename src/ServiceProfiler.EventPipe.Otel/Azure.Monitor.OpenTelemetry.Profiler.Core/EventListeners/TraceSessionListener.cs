#define CONSOLE_LOGGING
#undef CONSOLE_LOGGING    // Comment out this line for traces before the finishing of the constructor

using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;

internal class TraceSessionListener : EventListener
{
    // OpenTelemetry-SDK event source event ids.
    static class EventId
    {
        public const int RequestStart = 24;
        public const int RequestStop = 25;
    }

    public const string OpenTelemetrySDKEventSourceName = "OpenTelemetry-Sdk";
    private readonly ISerializationProvider _serializer;
    private readonly SampleCollector _sampleCollector;
    private readonly ILogger<TraceSessionListener> _logger;
    private readonly ManualResetEventSlim _ctorWaitHandle = new(false);
    private volatile bool _hasActivityReported = false;

    public SampleActivityContainer? SampleActivities => _sampleCollector?.SampleActivities;

    private readonly ConcurrentDictionary<string, byte> _startedActivityIds = new();

    public TraceSessionListener(
        ISerializationProvider serializer,
        SampleCollector sampleCollector,
        ILogger<TraceSessionListener> logger)
    {
        logger.LogTrace("Trace session listener ctor.");

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _sampleCollector = sampleCollector ?? throw new ArgumentNullException(nameof(sampleCollector));
        _ctorWaitHandle.Set();
        logger.LogTrace("Trace session listener created.");
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        // This event might trigger before the constructor is done.
        TryLogDebug($"Event source creating: {eventSource.Name}");
        // Dispatch this onto a different thread to avoid holding the thread to finish 
        // the constructor
        Task.Run(() =>
        {
            _ = HandleEventSourceCreated(eventSource).ConfigureAwait(false);
        });
        TryLogDebug($"Event source created: {eventSource.Name}");
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        _logger.LogTrace("OnEventWritten by {eventSource}", eventData.EventSource.Name);

        try
        {
            if (!string.Equals(eventData.EventSource.Name, OpenTelemetrySDKEventSourceName, StringComparison.Ordinal))
            {
                return;
            }

            OnRichPayloadEventWritten(eventData);
        }
        catch (Exception ex)
        {
            // We don't expect any exception here but if it happens, we still want to catch it and log it.
            // However, we don't want this to break the user's application.
            _logger.LogError(ex, "Unexpected exception happened.");
        }
    }

    /// <summary>
    /// Parses the rich payload EventSource event, adapter it and pump it into the Relay EventSource.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnRichPayloadEventWritten(EventWrittenEventArgs eventData)
    {
        _logger.LogTrace("[{Source}] {Action} - ActivityId: {activityId}, EventName: {eventName}, Keywords: {keyWords}, OpCode: {opCode}",
            eventData.EventSource.Name,
            nameof(OnRichPayloadEventWritten),
            eventData.ActivityId,
            eventData.EventName,
            eventData.Keywords,
            eventData.Opcode);

        if (string.IsNullOrEmpty(eventData.EventName))
        {
            return;
        }

        object? payload = eventData.Payload;
        if (payload is null)
        {
            _logger.LogWarning("Unexpected empty payload.");
            return;
        }

        if (string.Equals(eventData.EventSource.Name, OpenTelemetrySDKEventSourceName, StringComparison.Ordinal) &&
            (eventData.EventId == EventId.RequestStart || eventData.EventId == EventId.RequestStop))
        {
            string requestName = eventData.GetPayload<string>("name") ?? "Unknown";
            string id = eventData.GetPayload<string>("id") ?? throw new InvalidDataException("id payload is missing.");

            (string operationId, string requestId) = ExtractKeyIds(id);

            if (eventData.EventId == 24) // Started
            {
                HandleRequestStart(eventData, requestName, requestId, operationId, id);
                return;
            }

            if (eventData.EventId == 25) // Stopped
            {
                HandleRequestStop(eventData, requestName, requestId, operationId, id);
                return;
            }
        }
    }

    private async Task HandleEventSourceCreated(EventSource eventSource)
    {
        // This has to be the very first statement to making sure the objects are constructed.
        _ctorWaitHandle.Wait();

        try
        {
            _logger.LogDebug("Got manual trigger for source: {name}", eventSource.Name);

            await Task.Yield();
            if (string.Equals(eventSource.Name, OpenTelemetrySDKEventSourceName, StringComparison.OrdinalIgnoreCase))
            {
                EventKeywords keywordsMask = (EventKeywords)0x0000F;
                _logger.LogDebug("Enabling EventSource: {eventSourceName}", eventSource.Name);
                EnableEvents(eventSource, EventLevel.Verbose, keywordsMask);
            }
            else if (eventSource.Name == "System.Threading.Tasks.TplEventSource")
            {
                // Activity IDs aren't enabled by default.
                // Enabling Keyword 0x80 on the TplEventSource turns them on
                EnableEvents(eventSource, EventLevel.LogAlways, (EventKeywords)0x80);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error enabling event source: {name}", eventSource.Name);
        }
    }

    private bool IsInterestingRequest(string requestName)
    {
        // We only are interested capturing Http In requests.
        // Http request out, for example, from HttpClient will be excluded.
        return string.Equals("Microsoft.AspNetCore.Hosting.HttpRequestIn", requestName, StringComparison.Ordinal);
    }

    private void HandleRequestStart(EventWrittenEventArgs eventData, string requestName, string requestId, string operationId, string id)
    {
        Guid currentActivityId = eventData.ActivityId;
        bool isDebugLoggingEnabled = _logger.IsEnabled(LogLevel.Debug);
        if (isDebugLoggingEnabled)
        {
            _logger.LogDebug("Request started: Activity Id: {activityId}", currentActivityId);
        }

        if (!IsInterestingRequest(requestName))
        {
            if (isDebugLoggingEnabled)
            {
                _logger.LogDebug("Drop uninteresting request by name: {requestName}, id: {id}", requestName, id);
            }

            // Do not relay this event since it is not interesting.
            return;
        }

        // Interesting request

        if (isDebugLoggingEnabled)
        {
            _logger.LogDebug("Interesting start activity, name: {name}, id: {id}", requestName, id);
        }

        // Note to the _startedActivityIds bag, so that when stop happens, it knows to match.
        if (!_startedActivityIds.TryAdd(id, default))
        {
            _logger.LogWarning("Failed to add started activity. Activity by id {id} already exists? Please report a bug.", id);
        }

        AzureMonitorOpenTelemetryProfilerDataAdapterEventSource.Log.RequestStart(
            name: requestName,
            id: id,
            requestId: requestId,
            operationId: operationId);
    }

    private void HandleRequestStop(EventWrittenEventArgs eventData, string requestName, string requestId, string operationId, string id)
    {
        bool isDebugLoggingEnabled = _logger.IsEnabled(LogLevel.Debug);
        if (isDebugLoggingEnabled)
        {
            _logger.LogDebug("Request stopped: Activity Id: {activityId}", eventData.ActivityId);
        }

        if (!_startedActivityIds.TryRemove(id, out _))
        {
            if (isDebugLoggingEnabled)
            {
                _logger.LogDebug("No start activity found for this stop activity. Request name: {requestName}, id: {id}", requestName, id);
            }

            // No interesting start activity captured, it then doesn't make sense to just capture the stop.
            return;
        }

        if (isDebugLoggingEnabled)
        {
            _logger.LogDebug("Interesting activity found. Name: {name}, id: {id}", requestName, id);
        }
        // Interesting start activity was captured, relay this stop activity.
        AzureMonitorOpenTelemetryProfilerDataAdapterEventSource.Log.RequestStop(
            name: requestName, id: id, requestId: requestId, operationId: operationId);

        if (!_hasActivityReported && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Activity detected.");
            _hasActivityReported = true;
        }
    }

    public override void Dispose()
    {
        _ctorWaitHandle.Dispose();
        base.Dispose();
    }

    private void TryLogDebug(string message)
    {
        if (_logger is null)
        {
#if CONSOLE_LOGGING
            Console.WriteLine(message);
#endif
            return;
        }
        _logger.LogDebug(message);
    }

    // id example: 00-4dee62c12eaa9efca3d1f0565f3efda6-b3c470a7ee10c13b-01
    private static (string operationId, string requestId) ExtractKeyIds(string id)
    {
        string[] tokens = id.Split(['-'], StringSplitOptions.RemoveEmptyEntries);
        
        // It at least contains 3 parts.
        if (tokens.Length < 3)
        {
            throw new InvalidDataException(FormattableString.Invariant($"Id shall have at least 3 sections separated by `-`. Actual id: {id}"));
        }
        
        return (tokens[1], tokens[2]);
    }
}