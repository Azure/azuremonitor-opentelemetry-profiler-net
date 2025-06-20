using System;
using Azure.Core;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    /// <summary>
    /// A data contract to deserialize an <see ref= "AccessToken" /> object.
    /// </summary>
    internal class AccessTokenContract
    {
        public string Token { get; set; } = null!;
        public DateTimeOffset ExpiresOn { get; set; }

        public AccessToken ToAccessToken()
            => new AccessToken(Token, ExpiresOn);
    }
}
