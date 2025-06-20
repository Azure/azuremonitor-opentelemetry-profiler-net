// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Uploader;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    internal static class UploadContextExtensionTestHelper
    {
        public static UploadContext CreateUploadContext() =>
            new UploadContext()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://localhost"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampid",
                TraceFilePath = "/tmp/trace.etl",
                UploadMode = UploadMode.OnSuccess,
                PipeName = Guid.NewGuid().ToString(),
                RoleName = "testRoleName",
            };

        public static UploadContextExtension CreateUploadContextExtension() => new UploadContextExtension(CreateUploadContext());

    }
}
