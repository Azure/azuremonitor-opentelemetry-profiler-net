using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal class ActivityListValidator : TraceValidatorBase
    {
        public ActivityListValidator(
            string traceFilePath,
            ILogger<ActivityListValidator> logger,
            ITraceValidator? nextValidator) : base(logger, nextValidator)
        {
            if (string.IsNullOrEmpty(traceFilePath))
            {
                throw new ArgumentException($"'{nameof(traceFilePath)}' cannot be null or empty", nameof(traceFilePath));
            }

            _traceFilePath = traceFilePath;
            _sampleActivities = new ConcurrentDictionary<string, SampleActivityHolder>();
            _validSamples = new ConcurrentBag<SampleActivity>();
        }

        protected override IEnumerable<SampleActivity> ValidateImp(IEnumerable<SampleActivity> samples)
        {
            if (!samples.Any())
            {
                throw new ValidateFailedException(nameof(ActivityListValidator), "No sample activities to match.", toStopUploading: true);
            }

            foreach (SampleActivity sample in samples)
            {
                var holder = new SampleActivityHolder(sample);
                _sampleActivities.TryAdd(TraceEventOpcode.Start + sample.StartActivityIdPath, holder);
                _sampleActivities.TryAdd(TraceEventOpcode.Stop + sample.StopActivityIdPath, holder);
            }

            using (EventPipeEventSource eventPipeEventSource = new EventPipeEventSource(_traceFilePath))
            {
                _stopProcessingHandler = eventPipeEventSource.StopProcessing;
                eventPipeEventSource.AllEvents += EventPipeEventSource_AllEvents;
                try
                {
                    eventPipeEventSource.Process();
                }
                finally
                {
                    eventPipeEventSource.AllEvents -= EventPipeEventSource_AllEvents;
                    _stopProcessingHandler = null;
                }
            }

            if (_validSamples.IsEmpty)
            {
                throw new ValidateFailedException(nameof(ActivityListValidator), "No sample activity matches the trace.", true);
            }

            if (!_sampleActivities.IsEmpty)
            {
                _logger.LogDebug("All activities are not found in the trace. First one: {0}.", _sampleActivities.First().Key);
            }

            return _validSamples;
        }

        private void EventPipeEventSource_AllEvents(TraceEvent traceEvent)
        {
            if (traceEvent.Opcode == TraceEventOpcode.Start || traceEvent.Opcode == TraceEventOpcode.Stop)
            {
                string activityIdPath = traceEvent.ActivityID.GetActivityPath();
                string key = traceEvent.Opcode + activityIdPath;
                if (_sampleActivities.TryGetValue(key, out SampleActivityHolder? sampleHolder))
                {
                    if (traceEvent.Opcode == TraceEventOpcode.Start)
                    {
                        sampleHolder.StartActivityHit = true;
                    }
                    else if (traceEvent.Opcode == TraceEventOpcode.Stop)
                    {
                        sampleHolder.StopActivityHit = true;
                    }

                    _logger.LogTrace("[{0}] Hit on: {1}. OpCode: {2}, IsStartHit: {3}, IsStopHit: {4}",
                        DateTime.Now.ToLongTimeString(),
                        activityIdPath,
                        traceEvent.Opcode,
                        sampleHolder.StartActivityHit,
                        sampleHolder.StopActivityHit);

                    if (_sampleActivities.TryRemove(key, out sampleHolder))
                    {
                        if (sampleHolder.StartActivityHit && sampleHolder.StopActivityHit)
                        {
                            // Both start and stop are hit
                            _validSamples.Add(sampleHolder.SampleActivity);
                        }
                    }

                    if (_sampleActivities.IsEmpty)
                    {
                        _stopProcessingHandler?.Invoke();
                    }
                }
            }
        }

        private readonly string _traceFilePath;
        private Action? _stopProcessingHandler;

        // Samples to start with.
        private readonly ConcurrentDictionary<string, SampleActivityHolder> _sampleActivities;

        // Samples that hit start and stop events.
        private readonly ConcurrentBag<SampleActivity> _validSamples;
    }
}
