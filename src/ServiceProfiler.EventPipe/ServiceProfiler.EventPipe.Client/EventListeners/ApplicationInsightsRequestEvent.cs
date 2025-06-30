//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal class ApplicationInsightsRequestEvent : EventObjectBase
    {
        [JsonPropertyName("ActivityId")]
        public string ActivityId { get; set; } = null!;

        [JsonPropertyName("RelatedActivityId")]
        public string? RelatedActivityId { get; set; }

        [JsonPropertyName("Keywords")]
        public int Keywords { get; set; }

        [JsonPropertyName("TimeStamp")]
        public DateTimeOffset TimeStamp { get; set; }

        [JsonIgnore]
        public IDictionary<string, string>? Properties { get; set; }

        [JsonIgnore]
        public ApplicationInsightsDataRequestDataPayload? RequestDataPayload { get; set; }

        [JsonIgnore]
        public string? MachineName => Properties?["ai.cloud.roleInstance"];

        [JsonIgnore]
        public TimeSpan Duration => RequestDataPayload != null ? RequestDataPayload.Duration : TimeSpan.Zero;

        [JsonIgnore]
        public string? RequestId => RequestDataPayload?.Id;

        [JsonIgnore]
        public string? OperationName => RequestDataPayload?.Name ?? Properties?["ai.operation.name"];
        
        [JsonIgnore]
        public string? OperationId => Properties?["ai.operation.id"];
    }
}
