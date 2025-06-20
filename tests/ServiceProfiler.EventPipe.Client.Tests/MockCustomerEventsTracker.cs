////-----------------------------------------------------------------------------
//// Copyright (c) Microsoft Corporation.  All rights reserved.
////-----------------------------------------------------------------------------

//using System.Threading;
//using Microsoft.ApplicationInsights.DataContracts;
//using Microsoft.ApplicationInsights.Profiler.Core;
//using Microsoft.ApplicationInsights.Profiler.Core.SampleTransfers;
//using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
//using Microsoft.Extensions.Logging;
//using Microsoft.ServiceProfiler.Orchestration;

//namespace ServiceProfiler.EventPipe.Client.Tests
//{
//    class MockCustomerEventsTracker : CustomEventsTracker
//    {
//        public int CustomEventsSentCalled => _customEventsSent;
//        private int _customEventsSent;

//        public MockCustomerEventsTracker(
//            IServiceProfilerContext serviceProfilerContext,
//            ICustomTelemetryClientFactory customTelemetryClientFactory,
//            IResourceUsageSource resourceUsageSource,
//            ISerializationProvider serializer,
//            ILogger<CustomEventsTracker> logger)
//            : base(serviceProfilerContext, customTelemetryClientFactory, resourceUsageSource, serializer, logger)
//        {
//        }

//        protected override void SendCustomEventToAI(EventTelemetry telemetry)
//        {
//            Interlocked.Increment(ref _customEventsSent);
//        }
//    }
//}
