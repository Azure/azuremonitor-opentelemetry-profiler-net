//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.Json;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal sealed class TraceSessionListener30 : TraceSessionListener
    {
        public TraceSessionListener30(SampleActivityContainerFactory sampleActivityContainerFactory,
            ISerializationProvider serializer,
            ISerializationOptionsProvider<JsonSerializerOptions> serializerOptions,
            ILogger<TraceSessionListener30> logger)
            : base(sampleActivityContainerFactory, serializer, serializerOptions, logger)
        {
        }

        protected override void AlignCurrentThreadActivityIdImp(Guid activityId)
        {
            ApplicationInsightsDataRelayEventSource30.SetCurrentThreadActivityId(activityId);
        }

        protected override void RelayStartRequest(ApplicationInsightsOperationEvent operationEventData, Guid activityId)
        {
            AlignCurrentThreadActivityId(activityId);
            ApplicationInsightsDataRelayEventSource30.Log.RequestStart(
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

        protected override void RelayStopRequest(ApplicationInsightsOperationEvent operationEventData, long startTimeUTCTicks, Guid activityId)
        {
            AlignCurrentThreadActivityId(activityId);
            ApplicationInsightsDataRelayEventSource30.Log.RequestStop(
                operationEventData.EventId.ToString(CultureInfo.InvariantCulture),
                operationEventData.EventName,
                startTimeUTCTicks,
                operationEventData.TimeStamp.UtcTicks,
                requestId: operationEventData.RequestId,
                operationName: operationEventData.OperationName,
                machineName: Environment.MachineName,
                operationId: operationEventData.OperationId);
        }
    }
}
