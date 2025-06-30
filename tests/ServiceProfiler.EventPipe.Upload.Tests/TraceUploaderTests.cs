// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Microsoft.ApplicationInsights.Profiler.Uploader.Stubs;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Agent;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Contract.Agent;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class TraceUploaderTests
    {
        [Fact]
        public async Task ShouldZipTheTraceFileAsync()
        {
            bool isZipCalled = false;
            _zipUtilityMock.Setup(u => u.ZipFile(_traceFilePath, It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Callback(() =>
            {
                isZipCalled = true;
            });

            IServiceProvider serviceProvider = GetTestServiceProvider();
            TraceUploader uploader = new TraceUploader(
                serviceProvider.GetService<IZipUtility>(),
                serviceProvider.GetService<IBlobClientFactory>(),
                serviceProvider.GetService<IProfilerFrontendClientBuilder>(),
                serviceProvider.GetService<IAppInsightsLogger>(),
                serviceProvider.GetService<IOSPlatformProvider>(),
                serviceProvider.GetService<ITraceValidatorFactory>(),
                serviceProvider.GetService<ISampleActivitySerializer>(),
                serviceProvider.GetRequiredService<UploadContext>(),
                serviceProvider.GetRequiredService<IUploadContextValidator>(),
                serviceProvider.GetRequiredService<IAppProfileClientFactory>(),
                serviceProvider.GetRequiredService<ICustomEventsSender>(),
                serviceProvider.GetService<ILogger<TraceUploader>>());

            Assert.False(isZipCalled);
            await uploader.UploadAsync(CancellationToken.None);
            Assert.True(isZipCalled);
        }

        [Fact]
        public async Task ShouldUploadBlobAsync()
        {
            bool isBlobUploadCalled = false;
            const string zippedFilePath = "test_zipped.file";
            _zipUtilityMock.Setup(u => u.ZipFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns(zippedFilePath);

            IServiceProvider serviceProvider = GetTestServiceProvider(null, (path, cancellationToken) =>
            {
                isBlobUploadCalled = true;
                return Task.FromResult<Response<BlobContentInfo>>(null);
            });

            TraceUploader uploader = new TraceUploader(
                serviceProvider.GetService<IZipUtility>(),
                serviceProvider.GetService<IBlobClientFactory>(),
                serviceProvider.GetService<IProfilerFrontendClientBuilder>(),
                serviceProvider.GetService<IAppInsightsLogger>(),
                serviceProvider.GetService<IOSPlatformProvider>(),
                serviceProvider.GetService<ITraceValidatorFactory>(),
                serviceProvider.GetService<ISampleActivitySerializer>(),
                serviceProvider.GetRequiredService<UploadContext>(),
                serviceProvider.GetRequiredService<IUploadContextValidator>(),
                serviceProvider.GetRequiredService<IAppProfileClientFactory>(),
                serviceProvider.GetRequiredService<ICustomEventsSender>(),
                serviceProvider.GetService<ILogger<TraceUploader>>());

            Assert.False(isBlobUploadCalled);
            await uploader.UploadAsync(CancellationToken.None);
            Assert.True(isBlobUploadCalled);
        }

        [Fact]
        public async Task ShouldBlobSetMetadataAsync()
        {
            bool isSetMetadataAsyncCalled = false;
            IServiceProvider serviceProvider = GetTestServiceProvider(null, null, (metadata, conditions, cancellationToken) =>
            {
                Assert.Equal(Guid.Empty.ToString(), metadata[BlobMetadataConstants.DataCubeMetaName]);
                Assert.Equal(Environment.MachineName, metadata[BlobMetadataConstants.MachineNameMetaName], ignoreCase: true);
                Assert.Equal("C#", metadata[BlobMetadataConstants.ProgrammingLanguageMetaName]);
                Assert.Equal(nameof(TraceFileFormat.Netperf), metadata[BlobMetadataConstants.TraceFileFormatMetaName]);
                Assert.Equal("MockRoleName", metadata[BlobMetadataConstants.RoleName]);
                Assert.Equal("MockTrigger", metadata[BlobMetadataConstants.TriggerType]);
                isSetMetadataAsyncCalled = true;
                return Task.FromResult<Response<BlobInfo>>(null);
            });

            TraceUploader uploader = new TraceUploader(
                serviceProvider.GetService<IZipUtility>(),
                serviceProvider.GetService<IBlobClientFactory>(),
                serviceProvider.GetService<IProfilerFrontendClientBuilder>(),
                serviceProvider.GetService<IAppInsightsLogger>(),
                serviceProvider.GetService<IOSPlatformProvider>(),
                serviceProvider.GetService<ITraceValidatorFactory>(),
                serviceProvider.GetService<ISampleActivitySerializer>(),
                serviceProvider.GetRequiredService<UploadContext>(),
                serviceProvider.GetRequiredService<IUploadContextValidator>(),
                serviceProvider.GetRequiredService<IAppProfileClientFactory>(),
                serviceProvider.GetRequiredService<ICustomEventsSender>(),
                serviceProvider.GetService<ILogger<TraceUploader>>());

            Assert.False(isSetMetadataAsyncCalled);
            await uploader.UploadAsync(CancellationToken.None);
            Assert.True(isSetMetadataAsyncCalled);
        }

        [Fact]
        public async Task ShouldReportEtlUploadFinishAsync()
        {
            bool isReportEtlUploadFinishAsyncCalled = false;
            IServiceProvider serviceProvider = GetTestServiceProvider(() =>
            {
                isReportEtlUploadFinishAsyncCalled = true;
            });
            TraceUploader uploader = new TraceUploader(
                serviceProvider.GetService<IZipUtility>(),
                serviceProvider.GetService<IBlobClientFactory>(),
                serviceProvider.GetService<IProfilerFrontendClientBuilder>(),
                serviceProvider.GetService<IAppInsightsLogger>(),
                serviceProvider.GetService<IOSPlatformProvider>(),
                serviceProvider.GetService<ITraceValidatorFactory>(),
                serviceProvider.GetService<ISampleActivitySerializer>(),
                serviceProvider.GetRequiredService<UploadContext>(),
                serviceProvider.GetRequiredService<IUploadContextValidator>(),
                serviceProvider.GetRequiredService<IAppProfileClientFactory>(),
                serviceProvider.GetRequiredService<ICustomEventsSender>(),
                serviceProvider.GetService<ILogger<TraceUploader>>());

            Assert.False(isReportEtlUploadFinishAsyncCalled);
            await uploader.UploadAsync(CancellationToken.None);
            Assert.True(isReportEtlUploadFinishAsyncCalled);
        }

        [Fact]
        public void ShouldReturnNoValidActivityWhenValidationFails()
        {
            List<SampleActivity> input = new List<SampleActivity>(){
                new SampleActivity(),
            };

            Mock<ITraceValidator> exceptionValidator = new Mock<ITraceValidator>();
            exceptionValidator.Setup(v => v.Validate(It.IsAny<IEnumerable<SampleActivity>>())).Throws(new ValidateFailedException(toStopUploading: true));

            using (ServiceProvider provider = GetTestServiceProvider(traceValidatorFactory: () => exceptionValidator.Object))
            {
                TraceUploader target = ActivatorUtilities.CreateInstance<TraceUploader>(provider);
                IEnumerable<SampleActivity> output = target.GetValidSamples("abc.etl.zip", input);

                Assert.Equal(Enumerable.Empty<SampleActivity>(), output);
            }
        }

        private ServiceProvider GetTestServiceProvider(
            Action onReportEtlUploadFinished = null,
            Func<string, CancellationToken, Task<Response<BlobContentInfo>>> onBlobClientUploadAsync = null,
            Func<IDictionary<string, string>, BlobRequestConditions, CancellationToken, Task<Response<BlobInfo>>> onBlobClientSetMetadataAsync = null,
            Func<UploadContext> uploadContextFactory = null,
            Func<IUploadContextValidator> uploadContextValidatorFactory = null,
            Func<ITraceValidator> traceValidatorFactory = null)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<IZipUtility>(p => _zipUtilityMock.Object);
            _stampFrontendClientMock.Setup(s => s.GetStampIdAsync(It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult("test-stamp-id"));
            _stampFrontendClientMock.Setup(s => s.GetEtlUploadAccessAsync(It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_testBlobAccessPass));
            _stampFrontendClientMock.Setup(s => s.ReportEtlUploadFinishAsync(It.IsAny<StampBlobUri>(), It.IsAny<CancellationToken>()))
                .Callback(() => onReportEtlUploadFinished?.Invoke())
                .ReturnsAsync(true);

            services.AddTransient<IProfilerFrontendClient>(p => _stampFrontendClientMock.Object);
            services.AddTransient<IProfilerFrontendClientBuilder>(p =>
            {
                Mock<IProfilerFrontendClientBuilder> builderMock = new Mock<IProfilerFrontendClientBuilder>();
                builderMock.Setup(b => b.WithUploadContext(It.IsAny<UploadContextExtension>())).Returns(builderMock.Object);
                builderMock.Setup(b => b.Build()).Returns(p.GetRequiredService<IProfilerFrontendClient>());
                return builderMock.Object;
            });
            services.AddTransient<IAppInsightsLogger>(p => _telemetryLoggerMock.Object);

            var blobClientFactoryMock = new Mock<IBlobClientFactory>();
            blobClientFactoryMock.Setup(f => f.CreateBlobClient(It.IsAny<Uri>())).Returns(new MockBlobClient(new Uri(_testBlobUrl), onBlobClientUploadAsync, onBlobClientSetMetadataAsync));
            services.AddTransient<IBlobClientFactory>(p => blobClientFactoryMock.Object);
            services.AddSingleton<IOSPlatformProvider, OSPlatformProvider>();

            Mock<ITraceValidatorFactory> traceValidatorFactoryMock = new Mock<ITraceValidatorFactory>();
            traceValidatorFactoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(traceValidatorFactory?.Invoke() ?? new AlwaysPassValidator());
            services.AddTransient<ITraceValidatorFactory>(p => traceValidatorFactoryMock.Object);

            services.AddTransient<IPayloadSerializer, HighPerfJsonSerializationProvider>();

            Mock<ISampleActivitySerializer> sampleActivitySerializerMock = new Mock<ISampleActivitySerializer>();
            sampleActivitySerializerMock.Setup(s => s.SerializeToFileAsync(It.IsAny<IEnumerable<SampleActivity>>(), It.IsAny<string>())).Returns(Task.CompletedTask);
            services.AddTransient<ISampleActivitySerializer>(p => sampleActivitySerializerMock.Object);

            uploadContextFactory ??= CreateTestUploadContext;
            services.AddTransient<UploadContext>(p =>
            {
                return uploadContextFactory();
            });

            services.AddTransient<IUploadContextValidator>(p =>
            {
                uploadContextValidatorFactory = uploadContextValidatorFactory ?? (() => CreateUploadContextValidator(null));
                return uploadContextValidatorFactory();
            });

            services.AddTransient<IAppProfileClient>(p =>
            {
                Mock<IAppProfileClient> appProfileMock = new Mock<IAppProfileClient>();
                appProfileMock.Setup(m => m.GetAppProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Returns(() => Task.FromResult(new AppProfileResponse() { AppId = TraceUploaderTests._testAppId }));
                return appProfileMock.Object;
            });

            services.AddTransient<IAppProfileClientFactory>(p =>
            {
                Mock<IAppProfileClientFactory> factoryMock = new Mock<IAppProfileClientFactory>();
                factoryMock.Setup(f => f.Create(It.IsAny<UploadContextExtension>())).Returns(p.GetRequiredService<IAppProfileClient>());
                return factoryMock.Object;
            });

            Mock<ICustomEventsSender> customEventsSenderMock = new();
            services.AddTransient<ICustomEventsSender>(_ => customEventsSenderMock.Object);

            return services.BuildServiceProvider();
        }

        private readonly Mock<IZipUtility> _zipUtilityMock = new Mock<IZipUtility>();
        private readonly Mock<IProfilerFrontendClient> _stampFrontendClientMock = new Mock<IProfilerFrontendClient>();
        private readonly Mock<IAppInsightsLogger> _telemetryLoggerMock = new Mock<IAppInsightsLogger>();
        private const string _testSASToken = "this_is_a_test_sas_token";
        private const string _testBlobUrl = "https://this_is_blob_url";

        private const string _traceFilePath = "balabala";
        private static readonly Guid _testAppId = Guid.NewGuid();
        private BlobAccessPass _testBlobAccessPass = new BlobAccessPass { SASToken = _testSASToken, BlobUri = new Uri(_testBlobUrl) };

        private UploadContext CreateTestUploadContext()
        {
            return new UploadContext
            {
                TraceFilePath = _traceFilePath,
                UploadMode = UploadMode.Always,
                TriggerType = "MockTrigger",
                RoleName = "MockRoleName"
            };
        }

        private IUploadContextValidator CreateUploadContextValidator(Action<Mock<IUploadContextValidator>> mockConfigure = null)
        {
            Mock<IUploadContextValidator> uploadContextValidatorMock = new Mock<IUploadContextValidator>();
            mockConfigure = mockConfigure ?? (mock =>
            {
                mock.Setup(m => m.Validate(It.IsAny<UploadContext>())).Returns(string.Empty);
            });
            mockConfigure?.Invoke(uploadContextValidatorMock);

            return uploadContextValidatorMock.Object;
        }
    }
}
