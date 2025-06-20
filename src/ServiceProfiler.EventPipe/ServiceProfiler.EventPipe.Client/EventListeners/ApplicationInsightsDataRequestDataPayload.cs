//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Text.Json.Serialization;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal class ApplicationInsightsDataRequestDataPayload
    {
        [JsonPropertyName("ver")]
        public string Version { get; set; } = null!;

        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;
        
        [JsonPropertyName("duration")]
        public TimeSpan Duration { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;
    }
}
