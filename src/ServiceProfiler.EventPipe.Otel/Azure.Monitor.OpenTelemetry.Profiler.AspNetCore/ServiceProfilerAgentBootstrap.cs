// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;

namespace Azure.Monitor.OpenTelemetry.Profiler.AspNetCore;

internal class ServiceProfilerAgentBootstrap : IServiceProfilerAgentBootstrap
{
    private readonly IServiceProfilerContext _serviceProfilerContext;
    private readonly IOrchestrator _orchestrator;
    private readonly ServiceProfilerOptions _userConfiguration;
    private readonly ICompatibilityUtilityFactory _compatibilityUtilityFactory;
    private readonly ISerializationProvider _serializer;
    private readonly ILogger _logger;

    public ServiceProfilerAgentBootstrap(
        IServiceProfilerContext serviceProfilerContext,
        IOrchestrator orchestrator,
        IOptions<ServiceProfilerOptions> userConfiguration,
        ICompatibilityUtilityFactory compatibilityUtilityFactory,
        ISerializationProvider serializer,
        ILogger<ServiceProfilerAgentBootstrap> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userConfiguration = userConfiguration?.Value ?? throw new ArgumentNullException(nameof(userConfiguration));

        _compatibilityUtilityFactory = compatibilityUtilityFactory ?? throw new ArgumentNullException(nameof(compatibilityUtilityFactory));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serviceProfilerContext = serviceProfilerContext ?? throw new ArgumentNullException(nameof(serviceProfilerContext));
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    public async Task ActivateAsync(CancellationToken cancellationToken)
    {
        string noIKeyMessage = "No instrumentation key is set. Application Insights Profiler won't start.";

        if (_serializer.TrySerialize(_userConfiguration, out string? serializedUserConfiguration))
        {
            _logger.LogDebug("User Settings:{eol} {details}", Environment.NewLine, serializedUserConfiguration);
        }

        if (_userConfiguration.IsDisabled)
        {
            _logger.LogInformation("Service Profiler is disabled by the configuration.");
            return;
        }

        _logger.LogTrace("Starting service profiler from application builder.");
        (bool compatible, string reason) = _userConfiguration.IsSkipCompatibilityTest ? (true, "Skipped the compatibility test by settings.") : _compatibilityUtilityFactory.Create().IsCompatible();

        if (!compatible)
        {
            _logger.LogError("Compatibility test failed. Reason: {reason}" + Environment.NewLine +
                "Bypass the compatibility test by setting environment variable of ServiceProfiler__IsSkipCompatibilityTest to true.", reason);
            return;
        }

        if (!string.IsNullOrEmpty(reason)) { _logger.LogDebug(reason); }

        try
        {
            if (!_serviceProfilerContext.HasAppInsightsInstrumentationKey)
            {
                _logger.LogError(noIKeyMessage);
                return;
            }

            _logger.LogInformation("Starting application insights profiler with connection string: {connectionString}", _serviceProfilerContext.ConnectionString);
            await _orchestrator.StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            // Terminated.
            _logger.LogDebug("Profiler terminated by the user.");
        }
        catch (ArgumentNullException ex) when (string.Equals(ex.ParamName, "instrumentationKey", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Fail("You hit the safety net! How could it escape the instrumentation key check?");
            _logger.LogError(noIKeyMessage);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error.");
            if (_userConfiguration.AllowsCrash)
            {
                throw;
            }
        }
    }
}
