using Azure.Core;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.EventPipe.Upload.Tests.Mocks;

internal class TokenCredentialStub : TokenCredential
{
    private readonly AccessToken _accessToken;

    public TokenCredentialStub(AccessToken accessToken)
    {
        _accessToken = accessToken;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return _accessToken;
    }

    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return new ValueTask<AccessToken>(_accessToken);
    }
}
