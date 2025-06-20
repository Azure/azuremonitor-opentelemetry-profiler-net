//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class TraceUploaderTests : TestsBase
    {
        [Fact]
        public async Task UploadTraceWhenParametersAreGoodAsync()
        {
            IServiceProvider serviceProvider = GetRichServiceCollection().BuildServiceProvider();
            ITraceUploader target = new TraceUploaderProxy(serviceProvider.GetService<IUploaderPathProvider>(),
               serviceProvider.GetService<IFile>(),
               serviceProvider.GetService<IOutOfProcCallerFactory>(),
               serviceProvider.GetService<IServiceProfilerContext>(),
               GetLogger<TraceUploaderProxy>(),
               Options.Create(new UserConfiguration()),
               serviceProvider.GetService<IUploadContextValidator>(),
               serviceProvider.GetService<ITraceFileFormatDefinition>());

            UploadContextModel result = await target.UploadAsync(
                _testSessionId,
                _testTraceFilePath,
                metadataFilePath: null,
                sampleFilePath: @"c:\samples",
                namedPipeName: string.Empty,
                roleName: string.Empty,
                triggerType: string.Empty,
                cancellationToken: default);

            Assert.NotNull(result);
            Assert.Equal(_testIKey, result.AIInstrumentationKey);
            Assert.Equal(_testSessionId, result.SessionId);
            Assert.Equal(_testTraceFilePath, result.TraceFilePath);
            Assert.Equal(new Uri(_testServiceProfilerFrontendEndpoint), result.HostUrl);
        }

        [Fact]
        public async Task ShouldReturnNullUploadContextWhenUploadContextValidationFailedAsync()
        {
            IServiceProvider serviceProvider = GetRichServiceCollection().BuildServiceProvider();
            Mock<IUploadContextValidator> contextValidator = new Mock<IUploadContextValidator>();
            contextValidator.Setup(validator => validator.Validate(It.IsAny<UploadContextModel>())).Returns(() => "Validation failed.");
            ITraceUploader target = new TraceUploaderProxy(serviceProvider.GetService<IUploaderPathProvider>(),
                serviceProvider.GetService<IFile>(),
                serviceProvider.GetService<IOutOfProcCallerFactory>(),
                serviceProvider.GetService<IServiceProfilerContext>(),
                GetLogger<TraceUploaderProxy>(),
                Options.Create(new UserConfiguration()),
                contextValidator.Object,
               serviceProvider.GetService<ITraceFileFormatDefinition>());

            UploadContextModel result = await target.UploadAsync(
                _testSessionId,
                _testTraceFilePath,
                metadataFilePath: null,
                sampleFilePath: @"c:\samples",
                namedPipeName: string.Empty,
                roleName: string.Empty,
                triggerType: string.Empty,
                cancellationToken: default);

            // 1. No unhandled exception.
            // 2. Returned upload context is null.
            Assert.Null(result);
        }
    }
}
