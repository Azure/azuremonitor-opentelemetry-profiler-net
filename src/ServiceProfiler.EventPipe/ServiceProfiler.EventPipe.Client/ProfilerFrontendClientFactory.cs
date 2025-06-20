// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Utilities;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Core;

/// <summary>
/// This helper function simplifies the registration process for the <see cref="IProfilerFrontendClient" />.
/// Please do NOT inject this directly. Instead, inject the <see cref="IProfilerFrontendClient"/>.
/// </summary>
internal class ProfilerFrontendClientFactory
{
    private readonly IServiceProfilerContext _serviceProfilerContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly UserConfiguration _userConfiguration;
    private readonly IAuthTokenProvider _authTokenProvider;

    public ProfilerFrontendClientFactory(
        IAuthTokenProvider authTokenServiceFactory,
        IServiceProfilerContext serviceProfilerContext,
        IOptions<UserConfiguration> userConfiguration,
        ILoggerFactory loggerFactory)
    {
        _serviceProfilerContext = serviceProfilerContext ?? throw new System.ArgumentNullException(nameof(serviceProfilerContext));
        _loggerFactory = loggerFactory ?? throw new System.ArgumentNullException(nameof(loggerFactory));
        _userConfiguration = userConfiguration?.Value ?? throw new System.ArgumentNullException(nameof(userConfiguration));
        _authTokenProvider = authTokenServiceFactory ?? throw new System.ArgumentNullException(nameof(authTokenServiceFactory));
    }
    public IProfilerFrontendClient CreateProfilerFrontendClient()
    {

        TokenCredential? credential = _authTokenProvider.IsAADAuthenticateEnabled ?
            new AADAuthTokenCredential(
                _authTokenProvider,
                _loggerFactory.CreateLogger<AADAuthTokenCredential>()) :
            null;

        return new ProfilerFrontendClient(
            host: _serviceProfilerContext.StampFrontendEndpointUrl,
            instrumentationKey: _serviceProfilerContext.AppInsightsInstrumentationKey,
            machineName: _serviceProfilerContext.MachineName,
            featureVersion: "1.0.0",
            userAgent: FormattableString.Invariant($"ServiceProfilerEventPipeAgent/{EnvironmentUtilities.ExecutingAssemblyInformationalVersion}"),
            tokenCredential: credential,
            skipCertificateValidation: _userConfiguration.SkipEndpointCertificateValidation);
    }
}
