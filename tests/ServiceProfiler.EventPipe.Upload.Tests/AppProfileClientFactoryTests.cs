// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent;
using ServiceProfiler.EventPipe.Upload.Tests.Mocks;
using Xunit;
using static ServiceProfiler.EventPipe.Upload.Tests.UploadContextExtensionTestHelper;

namespace ServiceProfiler.EventPipe.Upload.Tests;

public class AppProfileClientFactoryTests : TestsBase
{
    [Fact]
    public void ShouldReturnIOptionsWithTokenCredential()
    {
        UploadContextExtension uploadContextExtension = CreateUploadContextExtension();
        uploadContextExtension.TokenCredential = new TokenCredentialStub(new AccessToken("jwttoken", expiresOn: DateTimeOffset.UtcNow.AddMinutes(5)));
        IngestionClientOptions ingestionClientOptions = new IngestionClientOptions();
        AppProfileClientFactory target = new AppProfileClientFactory(Options.Create(ingestionClientOptions), GetLoggerFactory());
        IOptions<IngestionClientOptions> result = target.CustomizeIngestionClientOptions(uploadContextExtension);

        Assert.NotNull(result);
        Assert.NotNull(result.Value.TokenCredential);
    }

    [Fact]
    public void ShouldReturnIOptionsWithoutTokenCredential()
    {
        UploadContextExtension uploadContextExtension = CreateUploadContextExtension();
        uploadContextExtension.TokenCredential = null; // No access token leads to ingestion client options without token credential
        IngestionClientOptions ingestionClientOptions = new IngestionClientOptions();
        AppProfileClientFactory target = new AppProfileClientFactory(Options.Create(ingestionClientOptions), GetLoggerFactory());
        IOptions<IngestionClientOptions> result = target.CustomizeIngestionClientOptions(uploadContextExtension);

        Assert.NotNull(result);
        Assert.Null(result.Value.TokenCredential);
    }

    [Fact]
    public void ShouldReturnIOptionsWithTokenCredentialByDefaultAccessToken()
    {
        UploadContextExtension uploadContextExtension = CreateUploadContextExtension();
        uploadContextExtension.TokenCredential = new TokenCredentialStub(accessToken: default); // Default access token leads to ingestion client options without token credential
        IngestionClientOptions ingestionClientOptions = new IngestionClientOptions();
        AppProfileClientFactory target = new AppProfileClientFactory(Options.Create(ingestionClientOptions), GetLoggerFactory());
        IOptions<IngestionClientOptions> result = target.CustomizeIngestionClientOptions(uploadContextExtension);

        Assert.NotNull(result);
        Assert.NotNull(result.Value.TokenCredential);
    }
}
