using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Core.Auth;

internal class AuthTokenProvider : IAuthTokenProvider
{
    private const string GetTokenAsyncMethodName = "GetTokenAsync";
    private const string CredentialEnvelopePropertyName = "CredentialEnvelope";

    /// <summary>
    /// Property accessor for the CredentialEnvelope property on <see cref="TelemetryConfiguration"/>.
    /// </summary>
    private static readonly PropertyInfo _credentialEnvelopePropertyInfo = typeof(TelemetryConfiguration).GetTypeInfo().GetDeclaredProperty(CredentialEnvelopePropertyName);

    /// <summary>
    /// Type info for CredentialEnvelope
    /// </summary>
    private static readonly TypeInfo? _credentialEnvelopeTypeInfo = _credentialEnvelopePropertyInfo?.PropertyType.GetTypeInfo();

    /// <summary>
    /// Method info for the GetTokenAsync method on the CredentialEnvelope.
    /// </summary>
    private static readonly MethodInfo? _getTokenAsyncMethodInfo = _credentialEnvelopeTypeInfo?.GetDeclaredMethod(GetTokenAsyncMethodName);

    /// <summary>
    /// Property accessor for the Result property on <see cref="Task{object}"/>.
    /// </summary>
    private static readonly PropertyInfo? _getTokenAsyncTaskResultPropertyInfo = _getTokenAsyncMethodInfo?.ReturnType.GetTypeInfo().GetDeclaredProperty(nameof(Task<object>.Result));

    private readonly TelemetryConfiguration _telemetryConfiguration;
    private readonly IAccessTokenFactory _authTokenFactory;
    private readonly ILogger _logger;

    public AuthTokenProvider(
        TelemetryConfiguration telemetryConfiguration,
        IAccessTokenFactory authTokenFactory,
        ILogger<AuthTokenProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _telemetryConfiguration = telemetryConfiguration ?? throw new ArgumentNullException(nameof(telemetryConfiguration));
        _authTokenFactory = authTokenFactory ?? throw new ArgumentNullException(nameof(authTokenFactory));
    }

    /// <inheritdoc />
    public bool IsAADAuthenticateEnabled => CredentialEnvelope != null;

    /// <inheritdoc />
    public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (CredentialEnvelope is null)
        {
            return default;
        }

        object? authTokenObject = await GetAuthTokenObjectAsync(cancellationToken).ConfigureAwait(false);

        if (authTokenObject is null)
        {
            return default;
        }

        if (!_authTokenFactory.TryCreateFrom(authTokenObject, out AccessToken authToken))
        {
            return default;
        }

        return authToken;
    }

    /// <summary>
    /// Gets an access token object using credential envelope, when exists.
    /// </summary>
    private async Task<object?> GetAuthTokenObjectAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsAADAuthenticateEnabled, "This shall not be called if AAD Authentication is not on.");
        Task authTokenTask = InvokeGetTokenAsync(cancellationToken);
        await authTokenTask.ConfigureAwait(false);
        return _getTokenAsyncTaskResultPropertyInfo?.GetValue(authTokenTask);
    }

    /// <summary>
    /// Invokes GetTokenAsync() on CredentialEnvelope object.
    /// </summary>
    private Task InvokeGetTokenAsync(CancellationToken cancellationToken)
    {
        if (_getTokenAsyncMethodInfo is null)
        {
            return Task.CompletedTask;
        }

        return (Task)_getTokenAsyncMethodInfo.Invoke(CredentialEnvelope, new object[] { cancellationToken });
    }

    /// <summary>
    /// Gets the CredentialEnvelop of <see cref="TelemetryConfiguration" />.
    /// </summary>
    /// <returns>Returns the CredentialEnvelope object when exists. Otherwise, null.</returns>
    private object? CredentialEnvelope => _credentialEnvelopePropertyInfo?.GetValue(_telemetryConfiguration);
}
