//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;

[assembly: SuppressMessage("Microsoft.Design", "CA1034: Nested types should not be visible", Justification = "Nested class for serialization.", Scope = "class", Target = "ServiceProfiler.EventPipe.Client.EventListeners.ApplicationInsightsDataRelayEventSource")]
namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{

    [EventSource(Name = EventSourceName, Guid = EventSourceGuidString)]
    internal class ApplicationInsightsDataRelayEventSource : EventSource
    {
        // Do NOT try to abstract event source. The Keywords, Tasks has to be nested classes. Otherwise, you will get 
        // Exceptions like 'Use of undefined keyword value 0x1 for event during runtime.'
#pragma warning disable CA1034
        public static class Keywords
#pragma warning restore CA1034

        {
            public const EventKeywords Request = (EventKeywords)0x1;
            public const EventKeywords Operations = (EventKeywords)0x400;
        }

#pragma warning disable CA1724 // The type name conflicts in whole or in part with the namespace name 'System.Threading.Tasks'.
#pragma warning disable CA1034 // Nested types should not be visible: Contract by EnventSource
        public static class Tasks
#pragma warning restore CA1034 // Nested types should not be visible
#pragma warning restore CA1724 // The type name conflicts in whole or in part with the namespace name 'System.Threading.Tasks'.
        {

            public const EventTask Request = (EventTask)0x1;
        }

#pragma warning disable CA1034 // Nested types should not be visible: Contract by EventSource
        public static class EventIds
#pragma warning restore CA1034 // Nested types should not be visible
        {
            public const int RequestStart = 1;
            public const int RequestStop = 2;
        }

        public const string EventSourceName = "Microsoft-ApplicationInsights-DataRelay";
        public const string EventSourceGuidString = "8206c581-c6a3-550a-af53-6e0421740cbe";

        public static ApplicationInsightsDataRelayEventSource Log { get; } = new ApplicationInsightsDataRelayEventSource();

        [Event(EventIds.RequestStart, Keywords = Keywords.Request, Level = EventLevel.Verbose, Opcode = EventOpcode.Start, Task = Tasks.Request, ActivityOptions = EventActivityOptions.None)]
        public void RequestStart(
            string id,
            string name,
            long startTimeTicks,
            long endTimeTicks,
            string requestId,
            string operationName,
            string machineName,
            string operationId)
        {
            WriteEvent(EventIds.RequestStart,
            id,
            name,
            startTimeTicks,
            endTimeTicks,
            requestId,
            operationName,
            machineName,
            operationId);
        }

        [Event(EventIds.RequestStop, Keywords = Keywords.Request, Level = EventLevel.Verbose, Opcode = EventOpcode.Stop, Task = Tasks.Request, ActivityOptions = EventActivityOptions.None)]
        public void RequestStop(
            string id,
            string name,
            long startTimeTicks,
            long endTimeTicks,
            string requestId,
            string operationName,
            string machineName,
            string operationId)
        {
            WriteEvent(EventIds.RequestStop,
            id,
            name,
            startTimeTicks,
            endTimeTicks,
            requestId,
            operationName,
            machineName,
            operationId);
        }
    }
}
