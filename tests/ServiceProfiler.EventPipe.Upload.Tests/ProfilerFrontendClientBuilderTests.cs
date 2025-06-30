// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Xunit;

using static ServiceProfiler.EventPipe.Upload.Tests.UploadContextExtensionTestHelper;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class ProfilerFrontendClientBuilderTests
    {
        [Fact]
        public void ShouldRequireCallWithUploadContext()
        {
            ProfilerFrontendClientBuilder target = new ProfilerFrontendClientBuilder();
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => target.Build());
            Assert.StartsWith(
                $"Call {nameof(ProfilerFrontendClientBuilder.WithUploadContext)} before calling {nameof(ProfilerFrontendClientBuilder.Build)}.",
                exception.Message);
        }

        [Fact]
        public void ShouldKeepChainByCallingWithUploadContext()
        {
            UploadContext uploadContext = CreateUploadContext();
            UploadContextExtension uploadContextExtension = new UploadContextExtension(uploadContext);

            ProfilerFrontendClientBuilder target = new ProfilerFrontendClientBuilder();
            IProfilerFrontendClientBuilder result = target.WithUploadContext(uploadContextExtension);

            Assert.NotNull(result);
            Assert.Same(target, result);
        }
    }
}
