// TODO: revisit this test.
////-----------------------------------------------------------------------------
//// Copyright (c) Microsoft Corporation.  All rights reserved.
////-----------------------------------------------------------------------------

//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Microsoft.ApplicationInsights.Extensibility;
//using Microsoft.ApplicationInsights.Profiler.Core;
//using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
//using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
//using Microsoft.ApplicationInsights.Profiler.Shared.Services;
//using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
//using Microsoft.Extensions.Options;
//using Microsoft.ServiceProfiler.Utilities;
//using Moq;
//using Xunit;

//namespace ServiceProfiler.EventPipe.Client.Tests
//{
//    public class ServiceProfilerContextTests : TestsBase
//    {
//        //[Fact]
//        //public void ShouldThrowWhenNoEndpointProviderSet()
//        //{
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());

//        //    using (var telemetryConfiguration = new TelemetryConfiguration())
//        //    {
//        //        ArgumentException ex = Assert.Throws<ArgumentNullException>(() =>
//        //        {
//        //            IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //                Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //                null,
//        //                userConfigurationOptions,
//        //                new AppInsightsProfileFetcher(_testEndpointUrl, new MockHttpMessageHandler()),
//        //                GetLogger<IServiceProfilerContext>());
//        //        });

//        //        Assert.Equal("endpointProvider", ex.ParamName);
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldSetEndpointByUserConfiguration()
//        //{
//        //    var endpointProviderMock = new Mock<IEndpointProvider>();
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration()
//        //    {
//        //        Endpoint = _testEndpointUrl,
//        //    });
//        //    using (var handler = new MockHttpMessageHandler())
//        //    using (var appInsightsProfileFetcher = new AppInsightsProfileFetcher(_testEndpointUrl, handler))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration())
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());
//        //        Assert.Equal(new Uri(_testEndpointUrl), serviceProfilerContext.StampFrontendEndpointUrl);
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldSetEndpointByEndpointProvider()
//        //{
//        //    var endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());
//        //    using (var handler = new MockHttpMessageHandler())
//        //    using (var appInsightsProfileFetcher = new AppInsightsProfileFetcher(_testEndpointUrl, handler))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration())
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());
//        //        Assert.Equal(new Uri(_testEndpointUrl), serviceProfilerContext.StampFrontendEndpointUrl);
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldSetEndpointByUserConfigurationIgnoreEndpointProvider()
//        //{
//        //    var endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration()
//        //    {
//        //        Endpoint = "https://endpointByUserConfiguration",
//        //    });
//        //    using (var handler = new MockHttpMessageHandler())
//        //    using (var appInsightsProfileFetcher = new AppInsightsProfileFetcher(_testEndpointUrl, handler))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration())
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());
//        //        Assert.Equal(new Uri("https://endpointByUserConfiguration"), serviceProfilerContext.StampFrontendEndpointUrl);
//        //    }
//        //}

//        //[Fact]
//        //public async Task ShouldCallToGetAppIdOnConstructorAsync()
//        //{
//        //    ISerializationProvider serializer = new HighPerfJsonSerializationProvider();
//        //    var endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());

//        //    TaskCompletionSource<bool> called = new TaskCompletionSource<bool>();
//        //    Guid actual = Guid.Empty;
//        //    using (var appInsightsProfileFetcher = CreateTestAppInsightsProfileFetcher(serializer))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = "InstrumentationKey=" + _testIKey.ToString() })
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());

//        //        serviceProfilerContext.AppIdFetched += (s, e) =>
//        //        {
//        //            actual = e.AppId;
//        //            called.SetResult(true);
//        //        };

//        //        if (serviceProfilerContext.AppInsightsAppId != Guid.Empty)
//        //        {
//        //            Assert.Equal(_testAppId, serviceProfilerContext.AppInsightsAppId);
//        //        }
//        //        else
//        //        {
//        //            Debug.Write("Waiting for fetch.");
//        //            Task finished = await Task.WhenAny(called.Task, Task.Delay(TimeSpan.FromSeconds(1)));
//        //            Assert.Equal(finished, called.Task);
//        //            Assert.Equal(_testAppId, actual);
//        //        }
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldParseIKey()
//        //{
//        //    ISerializationProvider serializer = new HighPerfJsonSerializationProvider();
//        //    Mock<IEndpointProvider> endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());

//        //    using (var appInsightsProfileFetcher = CreateTestAppInsightsProfileFetcher(serializer))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = "InstrumentationKey=" + _testIKey.ToString() })
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());

//        //        Assert.Equal(_testIKey, serviceProfilerContext.AppInsightsInstrumentationKey);
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldReturnGuidEmptyOnInvalidIKey()
//        //{
//        //    ISerializationProvider serializer = new HighPerfJsonSerializationProvider();
//        //    Mock<IEndpointProvider> endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());

//        //    using (var appInsightsProfileFetcher = CreateTestAppInsightsProfileFetcher(serializer))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = "InstrumentationKey=This is not a guid" })
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());

//        //        Assert.Equal(Guid.Empty, serviceProfilerContext.AppInsightsInstrumentationKey);
//        //    }
//        //}

//        //[Fact]
//        //public void ShouldReturnGuidEmptyWhenEmptyIKey()
//        //{
//        //    ISerializationProvider serializer = new HighPerfJsonSerializationProvider();
//        //    Mock<IEndpointProvider> endpointProviderMock = new Mock<IEndpointProvider>();
//        //    endpointProviderMock.Setup(m => m.GetEndpoint(It.IsAny<EndpointName>())).Returns(new Uri(_testEndpointUrl));
//        //    var userConfigurationOptions = Options.Create(new UserConfiguration());

//        //    using (var appInsightsProfileFetcher = CreateTestAppInsightsProfileFetcher(serializer))
//        //    using (var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = "InstrumentationKey=" + string.Empty })
//        //    {
//        //        IServiceProfilerContext serviceProfilerContext = new ServiceProfilerContext(
//        //            Options.Create<TelemetryConfiguration>(telemetryConfiguration),
//        //            endpointProviderMock.Object,
//        //            userConfigurationOptions,
//        //            appInsightsProfileFetcher,
//        //            GetLogger<IServiceProfilerContext>());

//        //        Assert.Equal(Guid.Empty, serviceProfilerContext.AppInsightsInstrumentationKey);
//        //    }
//        //}

//        // private const string _testEndpointUrl = "https://endpoint";
//    }
//}
