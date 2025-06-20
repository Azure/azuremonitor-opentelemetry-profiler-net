////-----------------------------------------------------------------------------
//// Copyright (c) Microsoft Corporation.  All rights reserved.
////-----------------------------------------------------------------------------

//using System;
//using System.Collections.Generic;
//using Microsoft.ApplicationInsights.Profiler.Core;
//using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
//using Microsoft.ApplicationInsights.Profiler.Core.SampleTransfers;
//using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.ServiceProfiler.Orchestration;
//using Xunit;

//namespace ServiceProfiler.EventPipe.Client.Tests
//{
//    public class CustomEventsTrackerTests : TestsBase
//    {
//        [Fact]
//        public void ShouldCheckRequiredParameter()
//        {
//            IServiceProvider serviceProvider = GetRichServiceCollection().BuildServiceProvider();

//            CustomEventsTracker target = new CustomEventsTracker(
//                serviceProvider.GetRequiredService<IServiceProfilerContext>(),
//                serviceProvider.GetRequiredService<ICustomTelemetryClientFactory>(),
//                serviceProvider.GetRequiredService<IResourceUsageSource>(),
//                serviceProvider.GetRequiredService<ISerializationProvider>(),
//                GetLogger<CustomEventsTracker>());

//            UploadContext uploadContext = CreateUploadContext();
//            ArgumentException ex = Assert.Throws<ArgumentNullException>(() => target.Send(null, uploadContext, 0, "UnitTest", Guid.NewGuid()));
//            Assert.Equal("samples", ex.ParamName);

//            List<SampleActivity> sampleActivities = new List<SampleActivity>();
//            ex = Assert.Throws<ArgumentNullException>(() => target.Send(sampleActivities, null, 0, "UnitTest", Guid.NewGuid()));
//            Assert.Equal("uploadContext", ex.ParamName);

//            ex = Assert.Throws<ArgumentException>(() => target.Send(sampleActivities, uploadContext, 0, null, Guid.NewGuid()));
//            Assert.Equal("profilingSource", ex.ParamName);

//            // No exception when all parameters are passed
//            target.Send(sampleActivities, uploadContext, 0, "UnitTest", Guid.NewGuid());
//        }

//        [Fact]
//        public void ShouldSendZeroEventsWhenNoSamples()
//        {
//            IServiceProvider serviceProvider = GetRichServiceCollection().BuildServiceProvider();

//            MockCustomerEventsTracker target = new MockCustomerEventsTracker(
//                serviceProvider.GetRequiredService<IServiceProfilerContext>(),
//                serviceProvider.GetRequiredService<ICustomTelemetryClientFactory>(),
//                serviceProvider.GetRequiredService<IResourceUsageSource>(),
//                serviceProvider.GetRequiredService<ISerializationProvider>(),
//                GetLogger<CustomEventsTracker>());

//            UploadContext uploadContext = CreateUploadContext();
//            List<SampleActivity> sampleActivities = new List<SampleActivity>();
//            int result = target.Send(sampleActivities, uploadContext, 0, "UnitTest", Guid.NewGuid());

//            Assert.Equal(0, target.CustomEventsSentCalled);
//            Assert.Equal(0, result);
//        }

//        [Fact]
//        public void ShouldSendSamples()
//        {
//            IServiceProvider serviceProvider = GetRichServiceCollection().BuildServiceProvider();

//            MockCustomerEventsTracker target = new MockCustomerEventsTracker(
//                serviceProvider.GetRequiredService<IServiceProfilerContext>(),
//                serviceProvider.GetRequiredService<ICustomTelemetryClientFactory>(),
//                serviceProvider.GetRequiredService<IResourceUsageSource>(),
//                serviceProvider.GetRequiredService<ISerializationProvider>(),
//                GetLogger<CustomEventsTracker>());

//            UploadContext uploadContext = CreateUploadContext();
//            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
//                new SampleActivity(),
//                new SampleActivity()
//            };
//            int result = target.Send(sampleActivities, uploadContext, 0, "UnitTest", Guid.NewGuid());

//            // An additional send is called for the index event.
//            Assert.Equal(2 + 1, target.CustomEventsSentCalled);
//            Assert.Equal(2, result);
//        }
//    }
//}
