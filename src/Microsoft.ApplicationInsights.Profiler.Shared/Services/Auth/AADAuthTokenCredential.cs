using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Auth;

/// <summary>
/// AAD auth token credential for application insights AAD authentication
/// </summary>
internal class AADAuthTokenCredential : TokenCredential
{
    private readonly IAuthTokenProvider _authTokenProvider;
    private readonly ILogger _logger;

    public AADAuthTokenCredential(IAuthTokenProvider authTokenProvider, ILogger<AADAuthTokenCredential> logger)
    {
        _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => AcquireTokenAsync(requestContext, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult();

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        => await AcquireTokenAsync(requestContext, cancellationToken).ConfigureAwait(false);

    private Task<AccessToken> AcquireTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        Debug.Assert(
            requestContext.Scopes != null &&
            requestContext.Scopes.Length == 1 &&
            string.Equals(requestContext.Scopes[0], "https://monitor.azure.com//.default", StringComparison.OrdinalIgnoreCase),
            "Unexpected target scope. Is there a misconfiguration? Check verbose logs.");
        _logger.LogTrace("Scopes: {@scopes}", requestContext.Scopes ?? []);
        return _authTokenProvider.GetTokenAsync(cancellationToken);
    }
}
