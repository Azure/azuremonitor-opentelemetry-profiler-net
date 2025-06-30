//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal abstract class EventObjectBase
    {
        [JsonPropertyName("EventName")]
        public string EventName { get; set; } = null!;

        [JsonPropertyName("EventId")]
        public int EventId { get; set; }

        [JsonPropertyName("Payload")]
        public object[] Payload { get; set; } = null!;
    }
}
