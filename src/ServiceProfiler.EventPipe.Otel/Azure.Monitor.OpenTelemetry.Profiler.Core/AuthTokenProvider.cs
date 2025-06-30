using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceProfiler.Common.Utilities;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class AuthTokenProvider(
    IOptions<ServiceProfilerOptions> serviceProfilerOptions,
    ConnectionString connectionString,
    ILogger<AuthTokenProvider> logger) : IAuthTokenProvider
{
    /// <summary>
    /// Default AAD Scope for Ingestion.
    /// IMPORTANT: This value only works in the Public Azure Cloud.
    /// For Sovereign Azure Clouds, this value MUST be built from the Connection String.
    /// </summary>
    public const string DefaultAadScope = "https://monitor.azure.com//.default";

    private readonly ServiceProfilerOptions _serviceProfilerOptions = serviceProfilerOptions?.Value ?? throw new ArgumentNullException(nameof(serviceProfilerOptions));
    private readonly ConnectionString _connectionString = connectionString;
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public bool IsAADAuthenticateEnabled => _serviceProfilerOptions.Credential is not null;


    public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
    {
        if(!IsAADAuthenticateEnabled)
        {
            return default;
        }

        TokenCredential? tokenCredential = _serviceProfilerOptions.Credential ?? throw new InvalidOperationException($"Credential is not provided. How does it pass the check of {nameof(IsAADAuthenticateEnabled)}?");
        string scope = GetScope();

        TokenRequestContext tokenRequestContext = new(scopes: [scope]);
        AccessToken accessToken = await tokenCredential.GetTokenAsync(tokenRequestContext, cancellationToken).ConfigureAwait(false);
        _logger.LogTrace("Access token: {token}", accessToken.Token);
        return accessToken;
    }

    /// <summary>
    /// Get the Scope value required for AAD authentication.
    /// </summary>
    private string GetScope()
    {
        string? audience = null;
        if (!string.IsNullOrEmpty(_serviceProfilerOptions.ConnectionString))
        {
            audience = _connectionString.TokenAudience;
        }
        return GetScope(audience);
    }

    /// <summary>
    /// Get the Scope value required for AAD authentication.
    /// </summary>
    /// <remarks>
    /// The AUDIENCE is a url that identifies Azure Monitor in a specific cloud (For example: "https://monitor.azure.com/").
    /// The SCOPE is the audience + the permission (For example: "https://monitor.azure.com//.default").
    /// </remarks>
    private static string GetScope(string? audience = null)
    {
        return string.IsNullOrWhiteSpace(audience)
            ? DefaultAadScope
            : audience + "/.default";
    }
}