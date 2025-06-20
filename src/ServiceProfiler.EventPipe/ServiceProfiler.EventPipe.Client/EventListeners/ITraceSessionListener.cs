//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using System;
using System.Diagnostics.Tracing;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal interface ITraceSessionListener : IDisposable
    {
        /// <summary>
        /// Enables the listener for the event source with a minimal level.
        /// </summary>
        /// <param name="eventSource">The event source.</param>
        /// <param name="level">The minimal level for the events to listen to.</param>
        void EnableEvents(EventSource eventSource, EventLevel level);

        /// <summary>
        /// Enables the listener for the event source with a minimal level and matched keywords.
        /// </summary>
        /// <param name="eventSource">The event source.</param>
        /// <param name="level">The minimal level for the events to listen to.</param>
        /// <param name="matchAnyKeyword">Keywords for matching.</param>
        void EnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword);

        /// <summary>
        /// The handler when a RichPayload Event is written.
        /// </summary>
        /// <param name="eventData">The payload on the event.</param>
        void OnRichPayloadEventWritten(EventWrittenEventArgs eventData);

        /// <summary>
        /// The collection of the activities gathered in a session.
        /// </summary>
        SampleActivityContainer SampleActivities { get; }

        /// <summary>
        /// Starts to relay the events to MS-AI-Data events provider.
        /// </summary>
        void Activate();

        /// <summary>
        /// Fires when start/stop activity failed match consecutively.
        /// </summary>
        event EventHandler<EventArgs> Poisoned;
    }
}