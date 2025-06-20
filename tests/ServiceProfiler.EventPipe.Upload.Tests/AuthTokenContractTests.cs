// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Xunit;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class AuthTokenContractTests : TestsBase
    {
        [Fact]
        public void ShouldBeAbleToDeserializeCorrect()
        {
            string accessToken = "hello-access-token";
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddMinutes(5);
            AccessToken toSerialize = new AccessToken(accessToken, expiresOn);
            ISerializationProvider serializer = new HighPerfJsonSerializationProvider();

            bool canSerialize = serializer.TrySerialize(toSerialize, out string serialized);
            Assert.True(canSerialize, "AccessToken should supports to be serialized.");

            if (canSerialize)
            {
                bool canDeserialized = serializer.TryDeserialize<AccessTokenContract>(serialized, out AccessTokenContract deserializedObject);
                Assert.True(canDeserialized, "AccessToken shall be able to be deserialized to AccessTokenContract");

                Assert.Equal(accessToken, deserializedObject.Token);
                Assert.Equal(expiresOn, deserializedObject.ExpiresOn);
            }
        }

        [Fact]
        public void ShouldBeAbleToCreateAccessToken()
        {
            string accessToken = "hello-access-token";
            DateTimeOffset expiresOn = DateTimeOffset.UtcNow.AddMinutes(5);

            AccessTokenContract target = new AccessTokenContract() {
                Token = accessToken,
                ExpiresOn = expiresOn,
            };

            AccessToken result = target.ToAccessToken();

            Assert.Equal(accessToken, result.Token);
            Assert.Equal(expiresOn, result.ExpiresOn);
        }
    }
}
