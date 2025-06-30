//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;
using Microsoft.ServiceProfiler.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text.Json;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal static class Extensions
    {
        public static ApplicationInsightsRequestEvent ToAppInsightsRequestEvent(
            this EventWrittenEventArgs eventData,
            ISerializationProvider serializer,
            ISerializationOptionsProvider<JsonSerializerOptions> serializationOptionsProvider)
        {
            if (serializer.TrySerialize(eventData, out string? serializedEventData))
            {
                bool isTargetDeserialized = serializer.TryDeserialize<ApplicationInsightsRequestEvent>(serializedEventData!, out ApplicationInsightsRequestEvent? target);

                if (isTargetDeserialized)
                {
                    Dictionary<string, string> properties = new Dictionary<string, string>();
                    foreach (JsonElement item in ((JsonElement)target!.Payload[1]).EnumerateArray())
                    {
                        properties.Add(item.GetProperty("Key").GetString()!, item.GetProperty("Value").GetString()!);
                    }

                    target.Properties = properties;

                    target.RequestDataPayload = ((JsonElement)target.Payload[2])
                        .Deserialize<ApplicationInsightsDataRequestDataPayload>(serializationOptionsProvider.Options);
                    return target;
                }
            }

            throw new UnsupportedPayloadTypeException("Can't serialize the eventData object.");
        }

        public static ApplicationInsightsOperationEvent ToAppInsightsOperationEvent(this EventWrittenEventArgs eventData, ISerializationProvider serializer)
        {
            if (serializer.TrySerialize(eventData, out string? serialized))
            {
                bool isDeserialized = serializer.TryDeserialize<ApplicationInsightsOperationEvent>(serialized!, out ApplicationInsightsOperationEvent? newEvent);
                if (isDeserialized)
                {
                    // Flatten properties for easier access
                    newEvent!.RequestId = newEvent.Payload[1].ToString();
                    // The actual operation name in the operation event data is always empty
                    Debug.Assert(string.IsNullOrEmpty(newEvent.OperationName), "Operation name is not empty. New chance for perf improvement!!!");
                    newEvent.OperationName = newEvent.Payload[2].ToString();
                    newEvent.OperationId = newEvent.Payload[3].ToString();

                    return newEvent;
                }
            }

            throw new UnsupportedPayloadTypeException("Can't serialize the eventData object.");
        }

        /// <summary>
        /// Converts an ActivitySample to <see cref="ArtifactLocationProperties"/>, makes it ready for transferring.
        /// </summary>
        /// <param name="sample">Activity sample.</param>
        /// <param name="stampId">StampId from Frontend client.</param>
        /// <param name="processId">Process ID.</param>
        /// <param name="sessionId">Session start time with offset.</param>
        /// <param name="dataCube">AppId.</param>
        /// <returns></returns>
        public static ArtifactLocationProperties ToArtifactLocationProperties(
            this SampleActivity sample,
            string stampId,
            int processId,
            DateTimeOffset sessionId,
            Guid dataCube,
            string machineName)
        {
            return new ArtifactLocationProperties(
                stampId,
                dataCube,
                machineName,
                processId,
                sample.StartActivityIdPath,
                sessionId,
                sample.StartTimeUtc,
                sample.StopTimeUtc);
        }
    }
}
