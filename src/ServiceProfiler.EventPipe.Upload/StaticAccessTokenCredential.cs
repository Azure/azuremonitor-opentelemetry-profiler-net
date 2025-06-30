using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal class StaticAccessTokenCredential : TokenCredential
    {
        private readonly AccessToken _accessToken;

        public StaticAccessTokenCredential(AccessToken accessToken)
        {
            _accessToken = accessToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => _accessToken;
        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            => new ValueTask<AccessToken>(_accessToken);
    }
}

