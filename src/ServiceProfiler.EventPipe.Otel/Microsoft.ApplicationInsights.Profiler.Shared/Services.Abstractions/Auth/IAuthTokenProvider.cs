using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;

/// <summary>
/// A service to get an authorization token for calling an authenticated endpoint.
/// </summary>
internal interface IAuthTokenProvider : IAADAuthChecker
{
    /// <summary>
    /// Gets an authorization token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The access token. See <ref="AccessToken" /> for details.</returns>
    Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken);
}
