// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Xunit;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class StaticAccessTokenCredentialTests
    {
        [Fact]
        public void ShouldBeAbleToGetToken()
        {
            AccessToken accessToken = new AccessToken("token", DateTimeOffset.UtcNow.AddMinutes(5));

            StaticAccessTokenCredential target = new StaticAccessTokenCredential(accessToken);

            AccessToken returnedToken = target.GetToken(default, default);

            Assert.Equal(accessToken, returnedToken);
        }

        [Fact]
        public async Task ShouldBeAbleToGetTokenAsync()
        {
            AccessToken accessToken = new AccessToken("token", DateTimeOffset.UtcNow.AddMinutes(5));

            StaticAccessTokenCredential target = new StaticAccessTokenCredential(accessToken);

            AccessToken returnedToken = await target.GetTokenAsync(default, default);

            Assert.Equal(accessToken, returnedToken);
        }
    }
}
