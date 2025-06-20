//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    static class EventName
    {
        public const string Request = "Request";
        public const string Operation = "Operation";
    }

    internal class TraceSessionListener : EventListener, ITraceSessionListener
    {
        public const string MicrosoftApplicationInsightsDataEventSourceName = "Microsoft-ApplicationInsights-Data";

        /// <summary>
        /// Gets or sets the sample activities.
        /// </summary>
        public SampleActivityContainer SampleActivities { get; private set; }

        public TraceSessionListener(SampleActivityContainerFactory sampleActivityContainerFactory,
            ISerializationProvider serializer,
            ISerializationOptionsProvider<JsonSerializerOptions> serializerOptionsProvider,
            ILogger<TraceSessionListener> logger)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serializerOptionsProvider = serializerOptionsProvider ?? throw new ArgumentNullException(nameof(serializerOptionsProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hasActivityReported = false;

            SampleActivities = sampleActivityContainerFactory.CreateNewInstance();
            _ctorWaitHandle.Set();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            Task.Run(() =>
            {
                if (_ctorWaitHandle != null)
                {
                    _ctorWaitHandle.Wait();
                    if (string.Equals(eventSource.Name, MicrosoftApplicationInsightsDataEventSourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        EventKeywords keywordsMask =
                            ApplicationInsightsDataRelayEventSource30.Keywords.Request |
                            ApplicationInsightsDataRelayEventSource30.Keywords.Operations;
                        _logger.LogDebug("[{0:O}] Enabling EventSource: {1}", DateTime.Now, eventSource.Name);
                        EnableEvents(eventSource, EventLevel.Verbose, keywordsMask);
                    }
                }
            }).ConfigureAwait(false);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (_isActivated && string.Equals(eventData.EventSource.Name,
                MicrosoftApplicationInsightsDataEventSourceName,
                StringComparison.Ordinal))
            {
                try
                {
                    OnRichPayloadEventWritten(eventData);
                }
                catch (Exception ex)
                {
                    // We don't expect any exception here but if it happens, we still want to catch it and log it.
                    // However, we don't want this to break the user's application.
                    _logger.LogError(ex, "Unexpected exception happened.");
                }
            }
        }

        /// <summary>
        /// Parses the rich payload EventSource event, adapter it and pump it into the Relay EventSource.
        /// </summary>
        /// <param name="eventData"></param>
        public void OnRichPayloadEventWritten(EventWrittenEventArgs eventData)
        {
            _logger.LogTrace("{0} - EventName: {1}, Keywords: {2}, OpCode: {3}",
                nameof(OnRichPayloadEventWritten),
                eventData.EventName,
                eventData.Keywords,
                eventData.Opcode);
            if (eventData.EventName.Equals(EventName.Request, StringComparison.Ordinal) && (eventData.Keywords.HasFlag(ApplicationInsightsDataRelayEventSource.Keywords.Operations)))
            {
                // Operation is sent, handle Start and Stop for it.
                ApplicationInsightsOperationEvent operationEventData = eventData.ToAppInsightsOperationEvent(_serializer);
                _logger.LogTrace("Request Activity (Start or Stop). Request Id: {requestId}.", operationEventData.RequestId);
                if (eventData.Opcode == EventOpcode.Start)
                {
                    Guid startActivityId = eventData.ActivityId;
                    RelayStartRequest(operationEventData, startActivityId);
                    // Getting activity id post relay
                    string startActivityPath = startActivityId.GetActivityPath();

                    // Record start time utc and start activity id.
                    SampleActivity result = _sampleActivityBuffer.AddOrUpdate(operationEventData.RequestId, new SampleActivity()
                    {
                        StartActivityIdPath = startActivityPath,
                        StartTimeUtc = operationEventData.TimeStamp,
                        RequestId = operationEventData.RequestId,
                        OperationId = operationEventData.OperationId,
                    }, (key, value) =>
                    {
                        value.StartActivityIdPath = startActivityPath;
                        value.StartTimeUtc = operationEventData.TimeStamp;
                        value.RequestId = operationEventData.RequestId;
                        Debug.Assert(string.Equals(value.OperationId, operationEventData.OperationId, StringComparison.Ordinal),
                            $"Start/Stop activity operation ids ({value.OperationId}/{operationEventData.OperationId}) should be the same.");
                        return value;
                    });

                    if (result == null)
                    {
                        _logger.LogWarning("Failed adding start activity: {0}, request id: {1}", startActivityPath, operationEventData.RequestId);
                    }
                }
                else if (eventData.Opcode == EventOpcode.Stop)
                {
                    // Getting activity id before relay.
                    Guid stopActivityId = eventData.ActivityId;
                    string stopActivityPath = stopActivityId.GetActivityPath();

                    SampleActivity result = _sampleActivityBuffer.AddOrUpdate(
                        operationEventData.RequestId,
                        new SampleActivity()
                        {
                            StopActivityIdPath = stopActivityPath,
                            StopTimeUtc = operationEventData.TimeStamp,
                            RequestId = operationEventData.RequestId,
                            OperationId = operationEventData.OperationId,
                        }, (key, value) =>
                        {
                            value.StopActivityIdPath = stopActivityPath;
                            value.StopTimeUtc = operationEventData.TimeStamp;
                            value.RequestId = operationEventData.RequestId;
                            Debug.Assert(string.Equals(value.OperationId, operationEventData.OperationId, StringComparison.Ordinal),
                                $"Start/Stop activity operation ids ({value.OperationId}/{operationEventData.OperationId}) should be the same.");
                            return value;
                        });

                    if (result != null)
                    {
                        RelayStopRequest(operationEventData, result.StartTimeUtc.UtcTicks, stopActivityId);
                    }
                }
            }
            else if (eventData.EventName.Equals(EventName.Request, StringComparison.Ordinal) && (eventData.Keywords.HasFlag(ApplicationInsightsDataRelayEventSource.Keywords.Request)))
            {
                ApplicationInsightsRequestEvent requestEventData = eventData.ToAppInsightsRequestEvent(_serializer, _serializerOptionsProvider);
                _logger.LogTrace("Request Activity. Request Id: {requestId}.", requestEventData.RequestId);

                SampleActivity targetRequest;

                string? requestId = requestEventData.RequestId;
                if (string.IsNullOrEmpty(requestId))
                {
                    return;
                }

                if (_sampleActivityBuffer.TryRemove(requestId!, out targetRequest))
                {
                    targetRequest.OperationName = requestEventData.OperationName;
                    targetRequest.Duration = requestEventData.Duration;

                    AppendSampleActivity(targetRequest);
                }
                else
                {
                    string message = "There is no matched start activity found for this request id: {0}. This could happen for the first few activities.";
                    if (!_hasActivityReported)
                    {
                        _logger.LogInformation(message, requestId);
                        _hasActivityReported = true;
                    }
                    else
                    {
                        _logger.LogDebug(message, requestId);
                    }
                }
            }
        }

        protected void AlignCurrentThreadActivityId(Guid activityId)
        {
            if (activityId != Guid.Empty)
            {
                AlignCurrentThreadActivityIdImp(activityId);
            };
        }

        protected virtual void AlignCurrentThreadActivityIdImp(Guid activityId)
        {
            ApplicationInsightsDataRelayEventSource.SetCurrentThreadActivityId(activityId);
        }

        protected virtual void RelayStopRequest(ApplicationInsightsOperationEvent operationEventData, long startTimeUTCTicks, Guid activityId)
        {
            AlignCurrentThreadActivityId(activityId);
            ApplicationInsightsDataRelayEventSource.Log.RequestStop(
                                        operationEventData.EventId.ToString(CultureInfo.InvariantCulture),
                                        operationEventData.EventName,
                                        startTimeUTCTicks,
                                        operationEventData.TimeStamp.UtcTicks,
                                        requestId: operationEventData.RequestId,
                                        operationName: operationEventData.OperationName,
                                        machineName: Environment.MachineName,
                                        operationId: operationEventData.OperationId);
        }

        protected virtual void RelayStartRequest(ApplicationInsightsOperationEvent operationEventData, Guid activityId)
        {
            AlignCurrentThreadActivityId(activityId);
            ApplicationInsightsDataRelayEventSource.Log.RequestStart(
                operationEventData.EventId.ToString(CultureInfo.InvariantCulture),
                operationEventData.EventName,
                operationEventData.TimeStamp.UtcTicks,
                // For start activity, endTime == startTime.
                operationEventData.TimeStamp.UtcTicks,
                requestId: operationEventData.RequestId,
                operationName: operationEventData.OperationName,
                machineName: Environment.MachineName,
                operationId: operationEventData.OperationId);
        }

        private void AppendSampleActivity(SampleActivity activity)
        {
            if (activity.IsValid(_logger))
            {
                // Send the AI CustomEvent
                try
                {
                    // No poison when any activity succeeded.
                    _poisonHit = 0;

                    if (SampleActivities.AddSample(activity))
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))  // Perf: Avoid serialization when not debugging.
                        {
                            bool isActivitySerialized = _serializer.TrySerialize(activity, out string? serializedActivity);
                            if (isActivitySerialized)
                            {
                                _logger.LogDebug("Sample is added: {0}", serializedActivity);
                            }
                            else
                            {
                                _logger.LogWarning("Serialize failed for activity: {0}", activity?.OperationId);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Fail to add activity into collection. Please making sure there's enough memory.");
                    }
                }
                catch (ObjectDisposedException ex)
                {
                    // activity builder has been disposed.
                    _logger.LogError(ex, "Start activity cache has been disposed before the activity is recorded.");
                }
                catch (InvalidOperationException ex)
                {
                    // The underlying collection was modified outside of this BlockingCollection<T> instance.
                    _logger.LogError(ex, "Invalid operation on start activity cache. Fail to record the activity.");
                }
            }
            else
            {
                _logger.LogInformation("Target request data is not valid upon receiving requests: {0}. This could happen for the first few activities.", activity.RequestId);
                if (_healthPoints <= 0)
                {
                    _isActivated = false;
                    OnPoisoned();
                }

                _healthPoints -= _poisonHit;
            }
        }

        public void Activate()
        {
            _isActivated = true;
        }

        public event EventHandler<EventArgs>? Poisoned;

        private void OnPoisoned()
        {
            Poisoned?.Invoke(this, EventArgs.Empty);
        }

        private bool _hasActivityReported;

        private bool _isActivated;
        private readonly ISerializationProvider _serializer;
        private readonly ISerializationOptionsProvider<JsonSerializerOptions> _serializerOptionsProvider;
        private readonly ILogger _logger;
        private readonly ManualResetEventSlim _ctorWaitHandle = new ManualResetEventSlim(false);

        private ConcurrentDictionary<string, SampleActivity> _sampleActivityBuffer = new ConcurrentDictionary<string, SampleActivity>();

        private short _healthPoints = 3;
        private short _poisonHit = 1;

        private bool _isDisposed = false;

        public override void Dispose()
        {
            base.Dispose();
            // Dispose of unmanaged resources.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) return;

            if (isDisposing)
            {
                _ctorWaitHandle?.Dispose();
            }

            _isDisposed = true;
        }
    }
}
