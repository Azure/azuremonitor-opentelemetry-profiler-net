//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.TraceControls;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Utilities;
using Moq;
using ServiceProfiler.Common.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public abstract class TestsBase
    {
        internal AppInsightsProfileFetcher CreateTestAppInsightsProfileFetcher(ISerializationProvider serializer)
        {
            var testProfile = new AppInsightsProfile { AppId = _testAppId, Location = "testlocation" };
            serializer.TrySerialize(testProfile, out string testProfileSerialized);
#pragma warning disable CA2000 // The delegate handler will be disposed alone with the AppInsightsProfileFetcher
            var mockHttpMessageHandler = new MockHttpMessageHandler();
#pragma warning restore CA2000 // The delegate handler will be disposed alone with the AppInsightsProfileFetcher.
            mockHttpMessageHandler.AvailableResponses.Add(
                $"{_testAppInsightsProfileEndpoint}/api/profiles/{_testIKey:D}",
                new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(testProfileSerialized)
                }
            );
            return new AppInsightsProfileFetcher(_testAppInsightsProfileEndpoint, mockHttpMessageHandler);
        }

        protected ILogger<T> GetLogger<T>()
            => new NullLogger<T>();

        protected IServiceCollection GetRichServiceCollection(
            TimeSpan? duration = null,
            TimeSpan? initialDelay = null,
            IServiceCollection serviceCollection = null,
            bool isFileExistResult = true,
            float cpuUsageAvg = 80,
            float memoryUsageAvg = 80)
        {
            duration = duration ?? TimeSpan.FromSeconds(1);
            initialDelay = initialDelay ?? TimeSpan.FromSeconds(1);

            serviceCollection = serviceCollection ?? BuildServiceCollection();

            serviceCollection.AddSingleton<IHostingEnvironment>(p =>
            {
                var hostingEnvironmentMock = new Mock<IHostingEnvironment>();
                hostingEnvironmentMock
                    .Setup(m => m.EnvironmentName)
                    .Returns("Hosting:UnitTestEnvironment");
                return hostingEnvironmentMock.Object;
            });

            ILogger<IServiceProfilerContext> profilerLogger = serviceCollection.BuildServiceProvider().GetService<ILogger<IServiceProfilerContext>>();

            // Role name detectors and sources
            Mock<IRoleNameSource> roleNameSourceMock = new();
            roleNameSourceMock.Setup(role => role.CloudRoleName).Returns("testCloudRoleName");
            serviceCollection.AddSingleton<IRoleNameSource>(_ => roleNameSourceMock.Object);

            // Role instance detectors and sources
            Mock<IRoleInstanceSource> roleInstanceSourceMock = new();
            roleInstanceSourceMock.Setup(role => role.CloudRoleInstance).Returns("testCloudRoleInstance");
            serviceCollection.AddSingleton<IRoleInstanceSource>(_ => roleInstanceSourceMock.Object);

            var delaySourceMock = new Mock<IDelaySource>();
            delaySourceMock.Setup(delay => delay.Delay(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            serviceCollection.AddTransient<IDelaySource>(p => delaySourceMock.Object);

            var expirationPolicyMock = new Mock<IExpirationPolicy>();
            expirationPolicyMock.Setup(policy => policy.IsExpired).Returns(false);
            serviceCollection.AddTransient<IExpirationPolicy>(p => expirationPolicyMock.Object);

            serviceCollection.AddTransient<SchedulingPolicy, MockSchedulingPolicy>();

            var aiSinksMock = new Mock<IAppInsightsSinks>();
            serviceCollection.AddSingleton<IAppInsightsSinks>(aiSinksMock.Object);

            var resourceUsageSourceMock = new Mock<IResourceUsageSource>();
            resourceUsageSourceMock.Setup(usage => usage.GetAverageCPUUsage()).Returns(cpuUsageAvg);
            resourceUsageSourceMock.Setup(usage => usage.GetAverageMemoryUsage()).Returns(memoryUsageAvg);
            serviceCollection.AddTransient<IResourceUsageSource>(p => resourceUsageSourceMock.Object);
            serviceCollection.AddTransient<IOptions<UserConfiguration>>(p => Options.Create(new UserConfiguration()));
            serviceCollection.AddTransient<IOptions<UserConfigurationBase>>(p =>
            {
                UserConfiguration userConfiguration = p.GetRequiredService<IOptions<UserConfiguration>>().Value;
                return Options.Create<UserConfigurationBase>(userConfiguration);
            });

            serviceCollection.AddTransient<IServiceProfilerContext>(provider =>
            {
                var serviceProfilerContextMock = new Mock<IServiceProfilerContext>();
                serviceProfilerContextMock.Setup(ctx => ctx.AppInsightsInstrumentationKey).Returns(_testIKey);
                serviceProfilerContextMock.Setup(ctx => ctx.MachineName).Returns("UnitTestMachineName");
                serviceProfilerContextMock.Setup(ctx => ctx.StampFrontendEndpointUrl).Returns(new Uri(_testServiceProfilerFrontendEndpoint));
                serviceProfilerContextMock.Setup(ctx => ctx.AppInsightsInstrumentationKey).Returns(() => _testIKey);
                serviceProfilerContextMock.Setup(ctx => ctx.ConnectionString).Returns(() =>
                {
                    ConnectionString.TryParse("InstrumentationKey=" + _testIKey, out ConnectionString connectionString);
                    return connectionString;
                });
                return serviceProfilerContextMock.Object;
            });

            serviceCollection.AddSingleton<DiagnosticsClientTraceConfiguration>();
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

            serviceCollection.AddTransient<IProfilerFrontendClient>(provider =>
            {
                _stampFrontendClientMock = new Mock<IProfilerFrontendClient>();
                _stampFrontendClientMock.Setup(s => s.GetStampIdAsync(It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(_testStampId));
                return _stampFrontendClientMock.Object;
            });

            serviceCollection.AddTransient<AppInsightsProfileFetcher>(provider => CreateTestAppInsightsProfileFetcher(provider.GetRequiredService<ISerializationProvider>()));

            serviceCollection.AddTransient<ITraceFileFormatDefinition>(_ => CurrentTraceFileFormat.Instance);

            var traceControlMock = new Mock<ITraceControl>();
            traceControlMock.Setup(tc => tc.SessionStartUTC).Returns(_testSessionId.UtcDateTime);
            serviceCollection.AddTransient<ITraceControl>(provider => traceControlMock.Object);

            serviceCollection.AddSingleton<IVersionProvider>(p => new VersionProvider(RuntimeInformation.FrameworkDescription, p.GetRequiredService<ILogger<IVersionProvider>>()));
            serviceCollection.AddSingleton<SampleActivityContainerFactory>();
            serviceCollection.AddTransient<ITraceSessionListenerFactory, TraceSessionListenerStubFactory>();

            var uploaderMock = new Mock<IOutOfProcCaller>();
            uploaderMock.Setup(uploader => uploader.ExecuteAndWait(It.IsAny<ProcessPriorityClass>())).Returns(0);

            Mock<IOutOfProcCallerFactory> outOfProcCallerFactoryMock = new Mock<IOutOfProcCallerFactory>();
            outOfProcCallerFactoryMock.Setup(factory => factory.Create(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(uploaderMock.Object);

            serviceCollection.AddTransient<IOutOfProcCaller>(provider => uploaderMock.Object);
            serviceCollection.AddTransient<IOutOfProcCallerFactory>(provider => outOfProcCallerFactoryMock.Object);

            serviceCollection.AddTransient<ICustomEventsBuilder, CustomEventsBuilder>();
            serviceCollection.AddSingleton<IPostStopProcessorFactory, PostStopProcessorFactory>();
            serviceCollection.AddSingleton<ServiceProfilerProvider>();

            _uploaderLocatorMock = new Mock<IPrioritizedUploaderLocator>();
            _uploaderLocatorMock.Setup(locater => locater.Locate()).Returns(@"C:\temp\Uploader.exe");
            serviceCollection.AddTransient<IPrioritizedUploaderLocator>(provider => _uploaderLocatorMock.Object);
            serviceCollection.AddTransient<IUploaderPathProvider, UploaderPathProvider>();

            var filesMock = new Mock<IFile>();
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(isFileExistResult);
            serviceCollection.AddTransient<IFile>(provider => filesMock.Object);

            var metadataWriterMock = new Mock<IMetadataWriter>();
            serviceCollection.AddTransient<IMetadataWriter>(provider => metadataWriterMock.Object);

            var telemetryTracker = new Mock<IEventPipeTelemetryTracker>();
            serviceCollection.AddTransient(provider => telemetryTracker.Object);

            _traceUploaderMock = new Mock<ITraceUploader>();
            UploadContextModel uploadContext = new UploadContextModel()
            {
                AIInstrumentationKey = _testIKey,
                HostUrl = new Uri(_testAppInsightsProfileEndpoint),
                SessionId = _testSessionId,
                StampId = _testStampId,
                TraceFilePath = _testTraceFilePath,
            };
            _traceUploaderMock.Setup(u => u.UploadAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>()))
                .Returns(() => Task.FromResult(uploadContext));
            serviceCollection.AddTransient<ITraceUploader>(provider => _traceUploaderMock.Object);

            serviceCollection.AddTransient<ISerializationProvider, HighPerfJsonSerializationProvider>();
            serviceCollection.AddTransient<ISerializationOptionsProvider<JsonSerializerOptions>, HighPerfJsonSerializationProvider>();

            // No validation error for uploadContext.
            Mock<IUploadContextValidator> uploadContextValidatorMock = new Mock<IUploadContextValidator>();
            uploadContextValidatorMock.Setup(validator => validator.Validate(It.IsAny<UploadContextModel>())).Returns(() => null);
            serviceCollection.AddTransient<IUploadContextValidator>(p => uploadContextValidatorMock.Object);

            Mock<IProfilerCoreAssemblyInfo> profilerCoreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            serviceCollection.AddTransient<IProfilerCoreAssemblyInfo>(p => profilerCoreAssemblyInfoMock.Object);

            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();
            userCacheManagerMock.Setup(m => m.TempTraceDirectory).Returns(new DirectoryInfo(Path.GetTempPath()));
            serviceCollection.AddTransient<IUserCacheManager>(p => userCacheManagerMock.Object);

            Mock<INamedPipeClientService> namedPipeClientServiceMock = new Mock<INamedPipeClientService>();
            namedPipeClientServiceMock.Setup(
                client => client.ReadAsync<Guid>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_testAppId));
            serviceCollection.AddTransient<INamedPipeClientService>(p => namedPipeClientServiceMock.Object);
            serviceCollection.AddTransient<IPayloadSerializer, HighPerfJsonSerializationProvider>();

            Mock<INamedPipeClientFactory> namedPipeClientFactoryMock = new Mock<INamedPipeClientFactory>();
            namedPipeClientFactoryMock.Setup(m => m.CreateNamedPipeService()).Returns(namedPipeClientServiceMock.Object);
            serviceCollection.AddTransient<INamedPipeClientFactory>(p => namedPipeClientFactoryMock.Object);

            serviceCollection.AddTransient<IRoleNameSource>(p =>
            {
                Mock<IRoleNameSource> roleNameSourceMock = new Mock<IRoleNameSource>();
                roleNameSourceMock.Setup(reader => reader.CloudRoleName).Returns("testCloudRoleName");
                return roleNameSourceMock.Object;
            });

            serviceCollection.AddTransient<IAuthTokenProvider>(p =>
            {
                Mock<IAuthTokenProvider> authTokenProviderMock = new Mock<IAuthTokenProvider>();
                authTokenProviderMock.Setup(authProvider => authProvider.GetTokenAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult<AccessToken>(default));
                return authTokenProviderMock.Object;
            });

            return serviceCollection;
        }

        internal UploadContextModel CreateUploadContext()
        {
            UploadContextModel uploadContext = new()
            {
                AIInstrumentationKey = _testIKey,
                HostUrl = new Uri(_testAppInsightsProfileEndpoint),
                SessionId = _testSessionId,
                StampId = _testStampId,
                TraceFilePath = _testTraceFilePath
            };
            return uploadContext;
        }

        protected const string _testStampId = "6fc442f4-80d0-47f3-9629-511c579c24bb";
        protected static readonly Guid _testIKey = Guid.Parse("9bd66e49-0082-41bf-a8c8-8f2d81534399");
        protected static readonly Guid _testAppId = Guid.Parse("ba9daf0d-8721-4ce3-91b7-6362f872f92b");
        protected const string _testAppInsightsProfileEndpoint = "https://dc.services.visualstudio.com";
        protected const string _testServiceProfilerFrontendEndpoint = "https://test-frontend.com";
        protected static readonly DateTimeOffset _testSessionId = DateTimeOffset.UtcNow;
        protected const string _testTraceFilePath = "/mnt/d/temp/trace.etl.zip";

        internal Mock<IProfilerFrontendClient> _stampFrontendClientMock;
        internal Mock<IPrioritizedUploaderLocator> _uploaderLocatorMock;
        internal Mock<ITraceUploader> _traceUploaderMock;

        private IServiceCollection BuildServiceCollection()
        {
            ServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(config => config.AddDebug().SetMinimumLevel(LogLevel.Debug));
            return serviceCollection;
        }
    }
}
