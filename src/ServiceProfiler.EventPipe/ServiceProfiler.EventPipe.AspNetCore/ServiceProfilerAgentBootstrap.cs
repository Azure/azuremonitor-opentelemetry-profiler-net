using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore;

internal class ServiceProfilerAgentBootstrap : IServiceProfilerAgentBootstrap
{
    private readonly IServiceProfilerContext _serviceProfilerContext;
    private readonly IOrchestrator _orchestrator;
    private readonly UserConfiguration _userConfiguration;
    private readonly ICompatibilityUtilityFactory _compatibilityUtilityFactory;
    private readonly ISerializationProvider _serializer;
    private readonly ILogger _logger;

    public ServiceProfilerAgentBootstrap(
        IServiceProfilerContext serviceProfilerContext,
        IOrchestrator orchestrator,
        IOptions<UserConfiguration> userConfiguration,
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
        string noConnectionStringMessage = "No connection string is set. Application Insights Profiler won't start.";

        bool isUserConfigSerialized = _serializer.TrySerialize(_userConfiguration, out string? serializedUserConfiguration);
        if (isUserConfigSerialized)
        {
            _logger.LogDebug("User Settings:" + Environment.NewLine + "{details}", serializedUserConfiguration);
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

        // Check if any of these diagnostic settings are disabled
        // https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables#dotnet_enablediagnostics
        foreach (string item in DiagnosticsVariables.GetAllVariables())
        {
            if (Environment.GetEnvironmentVariable(item) == "0")
            {
                _logger.LogError("{variable} is set to 0. Profiler is disabled", item);
                return;
            }
        }

        if (!string.IsNullOrEmpty(reason)) { _logger.LogDebug(reason); }

        try
        {
            // Connection string exists.
            if (string.IsNullOrEmpty(_serviceProfilerContext.ConnectionString?.ToString()))
            {
                _logger.LogError(noConnectionStringMessage);
                return;
            }

            // Instrumentation key is well-formed.
            if (_serviceProfilerContext.AppInsightsInstrumentationKey == Guid.Empty)
            {
                _logger.LogError("Instrumentation key is not set or malformed in the connection string. Application Insights Profiler won't start.");
                return;
            }

            _logger.LogInformation("Starting application insights profiler with instrumentation key: {iKey}", _serviceProfilerContext.AppInsightsInstrumentationKey);
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
            _logger.LogError(noConnectionStringMessage);
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
