//-----------------------------------------------------------------------------
// Copyright(c) Microsoft Corporation.All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.AspNetCore;
using Microsoft.ApplicationInsights.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.TraceControls;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Utilities;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class ServiceProfilerExtensionTests : TestsBase
    {
        [Fact]
        public void ShouldThrowWhenNoIKeyGiven()
        {
            IServiceCollection serviceCollection = CreateInitialServiceCollection();

            serviceCollection = serviceCollection.AddServiceProfiler(serviceCollectionBuilder: new NoIKeyTestServiceCollectionBuilder());

            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            ServiceProfilerServices profilerServices = serviceProvider.GetRequiredService<ServiceProfilerServices>();
            profilerServices.ServicesInitialized += (s, e) =>
            {
                // However, the type will not be injected into the service provider.
                Assert.Throws<ArgumentNullException>(() => serviceProvider.GetRequiredService<ServiceProfilerProvider>());
            };

        }

        [Fact]
        public void ShouldInjectTheService()
        {
            IServiceCollection serviceCollection = CreateInitialServiceCollection();
            serviceCollection = serviceCollection.AddServiceProfiler(
                serviceCollectionBuilder: new TestServiceCollectionBuilder());
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            ServiceProfilerServices profilerServices = serviceProvider.GetRequiredService<ServiceProfilerServices>();
            profilerServices.ServicesInitialized += (s, e) =>
            {
                ServiceProfilerProvider service = serviceProvider.GetRequiredService<ServiceProfilerProvider>();
                Assert.NotNull(service);
            };
        }

        [Fact]
        public void ShouldBeAbleToCustomizeProfilerByOptions()
        {
            IServiceCollection serviceCollection = CreateInitialServiceCollection();

            serviceCollection = serviceCollection.AddServiceProfiler(options =>
            {
                options.BufferSizeInMB = 100;
                options.Duration = TimeSpan.FromSeconds(1);
                options.InitialDelay = TimeSpan.FromSeconds(3);
            });

            IServiceProvider provider = serviceCollection.BuildServiceProvider();
            DiagnosticsClientTraceConfiguration traceConfiguration = provider.GetRequiredService<DiagnosticsClientTraceConfiguration>();
            Assert.Equal(100, traceConfiguration.CircularBufferMB);

            SchedulingPolicy traceSchedule = provider.GetRequiredService<SchedulingPolicy>();
            Assert.Equal(TimeSpan.FromSeconds(1), traceSchedule.ProfilingDuration);
        }

        [Fact]
        public void ShouldBeAbleToCustomizeProfilerByConfigurationOverwrites()
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string>(){
                    {"a" , "b"}, // noise
                    {"ServiceProfiler:BufferSizeInMB", "100"},
                    {"ServiceProfiler:Duration", "00:01:15"},
                    {"ServiceProfiler:InitialDelay", "00:00:18"},
            }).Build();

            IServiceCollection serviceCollection = CreateInitialServiceCollection();

            serviceCollection = serviceCollection.AddServiceProfiler(config);

            IServiceProvider provider = serviceCollection.BuildServiceProvider();
            DiagnosticsClientTraceConfiguration traceConfiguration = provider.GetRequiredService<DiagnosticsClientTraceConfiguration>();
            Assert.Equal(100, traceConfiguration.CircularBufferMB);

            SchedulingPolicy traceSchedule = provider.GetRequiredService<SchedulingPolicy>();
            Assert.Equal(TimeSpan.FromSeconds(75), traceSchedule.ProfilingDuration);
        }

        [Fact]
        public void ShouldBeAbleToCustomizeProfilerByConfigurations()
        {
            IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string>(){
                    { "APPLICATIONINSIGHTS_CONNECTION_STRING", "InstrumentationKey=8e321e78-834f-4397-96fd-297fa844d140" },
                    {"a" , "b"}, // noise
                    {"ServiceProfiler:BufferSizeInMB", "100"},
                    {"ServiceProfiler:Duration", "00:01:15"},
                    {"ServiceProfiler:InitialDelay", "00:00:18"},
            }).Build();

            IServiceCollection serviceCollection = CreateInitialServiceCollection(config);
            serviceCollection.AddSingleton(config);

            serviceCollection = serviceCollection.AddServiceProfiler();

            IServiceProvider provider = serviceCollection.BuildServiceProvider();
            DiagnosticsClientTraceConfiguration traceConfiguration = provider.GetRequiredService<DiagnosticsClientTraceConfiguration>();
            Assert.Equal(100, traceConfiguration.CircularBufferMB);

            SchedulingPolicy schedulingPolicy = provider.GetRequiredService<SchedulingPolicy>();
            Assert.Equal(TimeSpan.FromSeconds(75), schedulingPolicy.ProfilingDuration);
        }

        // TODO: Cover more scenarios around IAppInsightsLoggers
        [Fact]
        public void ShouldAllowUserToOptOutTelemetry()
        {
            string customerIKey = _testIKey.ToString();
            IServiceCollection serviceCollection = CreateInitialServiceCollection();
            serviceCollection = serviceCollection.AddServiceProfiler(options =>
            {
                options.ProvideAnonymousTelemetry = false;
            });

            TelemetryConfiguration telemetryConfiguration = serviceCollection.BuildServiceProvider().GetRequiredService<TelemetryConfiguration>();
            telemetryConfiguration.ConnectionString = $"InstrumentationKey={customerIKey}";
            ServiceDescriptor telemetryConfigurationDescriptor = serviceCollection.First(d => d.ServiceType == typeof(TelemetryConfiguration));
            serviceCollection.Remove(telemetryConfigurationDescriptor);
            serviceCollection.AddSingleton<TelemetryConfiguration>(telemetryConfiguration);

            // Mock endpoint provider
            ServiceDescriptor endpointProviderDescriptor = serviceCollection.FirstOrDefault(d => d.ServiceType is IEndpointProvider);
            serviceCollection.Remove(endpointProviderDescriptor);
            var endpointProviderMock = new Mock<IEndpointProvider>();
            endpointProviderMock.Setup(e => e.GetEndpoint()).Returns(new Uri(_testAppInsightsProfileEndpoint));
            serviceCollection.AddTransient<IEndpointProvider>(p => endpointProviderMock.Object);

            // Build service provider
            IServiceProvider provider = serviceCollection.BuildServiceProvider();

            var targetLogger = provider.GetServices<IAppInsightsLogger>().First(l => l.ConnectionString == null || !l.ConnectionString.ToString().Contains(customerIKey));
            Assert.Equal(typeof(NullAppInsightsLogger), targetLogger.GetType());
        }

        [Fact]
        public void ShouldInjectMultipleIAppInsightsLoggerServices()
        {
            ServiceCollection serviceCollection = CreateInitialServiceCollection();
            serviceCollection.AddServiceProfiler();
            int count = serviceCollection.Where(item => item.ServiceType == typeof(IAppInsightsLogger)).Count();
            Assert.True(count >= 2);
        }

        #region Private
        private ServiceCollection CreateInitialServiceCollection(IConfiguration customConfiguration = null)
        {
            ServiceCollection serviceCollection = new ServiceCollection();

            IConfiguration config = customConfiguration ?? new ConfigurationBuilder().AddInMemoryCollection([
                new("APPLICATIONINSIGHTS_CONNECTION_STRING", "InstrumentationKey=8e321e78-834f-4397-96fd-297fa844d140"),
            ]).Build();

            serviceCollection.AddSingleton<IConfiguration>(config);
            serviceCollection.AddSingleton<IHostingEnvironment>(p =>
            {
                var hostingEnvironmentMock = new Mock<IHostingEnvironment>();
                hostingEnvironmentMock
                    .Setup(m => m.EnvironmentName)
                    .Returns("Hosting:UnitTestEnvironment");
                return hostingEnvironmentMock.Object;
            });

            return serviceCollection;
        }

        private static readonly Guid testIKey = Guid.Parse("9bd66e49-0082-41bf-a8c8-8f2d81534399");
        private static readonly Guid testAppId = Guid.Parse("ba9daf0d-8721-4ce3-91b7-6362f872f92b");
        private const string TestAppInsightsProfileEndpoint = "https://dc.services.visualstudio.com";

        private class TestServiceCollectionBuilder : IServiceCollectionBuilder
        {
            public virtual IServiceCollection Build(IServiceCollection serviceCollection)
            {
                UserConfiguration options = new UserConfiguration();
                return CreateServiceCollection(options.Duration, options.InitialDelay, serviceCollection, isIKeyNull: false);
            }

            protected IServiceCollection CreateServiceCollection(
                TimeSpan duration,
                TimeSpan initialDelay,
                IServiceCollection inCollection,
                Action traceControlEnableCallback = null,
                Action traceControlDisableCallback = null,
                Action setupStampFrontendCallback = null,
                Action uploaderExecuteCallback = null,
                bool isIKeyNull = false)
            {
                IServiceCollection serviceCollection = inCollection ?? new ServiceCollection();
                serviceCollection.AddLogging(config => config.AddDebug().SetMinimumLevel(LogLevel.Debug));

                ILogger<IServiceProfilerContext> profilerLogger = serviceCollection.BuildServiceProvider().GetService<ILogger<IServiceProfilerContext>>();

                serviceCollection.AddTransient<IEndpointProvider>(p =>
                {
                    var endpointProviderMock = new Mock<IEndpointProvider>();
                    return endpointProviderMock.Object;
                });

                var aiOptions = new ApplicationInsightsServiceOptions();
                serviceCollection.AddSingleton<IOptions<ApplicationInsightsServiceOptions>>(Options.Create(aiOptions));

                var defaultUserConfig = new UserConfiguration();
                serviceCollection.AddSingleton<IOptions<UserConfiguration>>(Options.Create(defaultUserConfig));

                serviceCollection.AddTransient<ServiceProfilerContext>();

                serviceCollection.AddTransient<DiagnosticsClientTraceConfiguration>();

                IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

                // HttpClientHandler that  by passes certificate validation for SSL for test purpose.
                serviceCollection.AddSingleton<HttpClientHandler>(p =>
                {
                    HttpClientHandler handler = new HttpClientHandler();
#if DEBUG
                    handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
#endif
                    return handler;
                });

                var stampFrontendClientMock = new Mock<IProfilerFrontendClient>();
                serviceCollection.AddTransient<IProfilerFrontendClient>(provider => stampFrontendClientMock.Object);

                serviceCollection.AddTransient<AppInsightsProfileFetcher>(provider => CreateTestAppInsightsProfileFetcher());

                var traceControlMock = new Mock<ITraceControl>();
                if (traceControlEnableCallback != null)
                {
                    traceControlMock.Setup(c => c.EnableAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Callback(traceControlEnableCallback);
                }

                if (traceControlDisableCallback != null)
                {
                    traceControlMock.Setup(c => c.DisableAsync(It.IsAny<CancellationToken>())).Callback(traceControlDisableCallback);
                }

                serviceCollection.AddTransient<ITraceControl>(provider => traceControlMock.Object);
                serviceCollection.AddSingleton<SampleActivityContainerFactory>();
                //serviceCollection.AddTransient<ITraceSessionListenerFactory, TraceSessionListenerStubFactory>();

                var uploaderMock = new Mock<IOutOfProcCaller>();
                if (uploaderExecuteCallback != null)
                {
                    uploaderMock.Setup(uploader => uploader.ExecuteAndWait(ProcessPriorityClass.Normal)).Callback(uploaderExecuteCallback);
                }

                serviceCollection.AddTransient<IOutOfProcCaller>(provider => uploaderMock.Object);
                serviceCollection.AddSingleton<ServiceProfilerProvider>();

                var uploaderLocatorMock = new Mock<IPrioritizedUploaderLocator>();
                uploaderLocatorMock.Setup(locater => locater.Locate()).Returns(@"C:\temp\Uploader.exe");
                serviceCollection.AddTransient<IPrioritizedUploaderLocator>(provider => uploaderLocatorMock.Object);

                var filesMock = new Mock<IFile>();
                filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
                serviceCollection.AddTransient<IFile>(provider => filesMock.Object);

                var telemetryTracker = new Mock<IEventPipeTelemetryTracker>();
                serviceCollection.AddTransient(provider => telemetryTracker.Object);

                return serviceCollection;
            }

            private IServiceProvider CreateServiceProvider(
            TimeSpan duration,
            TimeSpan initialDelay,
            Action traceControlEnableCallback = null,
            Action traceControlDisableCallback = null,
            Action setupStampFrontendCallback = null,
            Action uploaderExecuteCallback = null,
            bool isIKeyNull = false)
            {
                IServiceProvider serviceProvider = CreateServiceCollection(
                    duration, initialDelay, null,
                    traceControlEnableCallback, traceControlDisableCallback,
                    setupStampFrontendCallback, uploaderExecuteCallback,
                    isIKeyNull).BuildServiceProvider();
                return serviceProvider;
            }

            private AppInsightsProfileFetcher CreateTestAppInsightsProfileFetcher()
            {
#pragma warning disable CA2000 // The delegate handler will be disposed alone with the AppInsightsProfileFetcher.
                var mockHttpMessageHandler = new MockHttpMessageHandler();
#pragma warning restore CA2000 // The delegate handler will be disposed alone with the AppInsightsProfileFetcher.
                mockHttpMessageHandler.AvailableResponses.Add(
                    $"{TestAppInsightsProfileEndpoint}/api/profiles/{testIKey:D}/appId",
                    new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                    {
                        Content = new StringContent(testAppId.ToString("D"))
                    }
                );

                return new AppInsightsProfileFetcher(TestAppInsightsProfileEndpoint, mockHttpMessageHandler);
            }
        }

        private class NoIKeyTestServiceCollectionBuilder : TestServiceCollectionBuilder, IServiceCollectionBuilder
        {
            public override IServiceCollection Build(IServiceCollection serviceCollection)
            {
                UserConfiguration options = new UserConfiguration();
                return CreateServiceCollection(options.Duration, options.InitialDelay, serviceCollection, isIKeyNull: true);
            }
        }
        #endregion
    }
}
