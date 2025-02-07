//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.Exceptions;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;
using Microsoft.ServiceProfiler.Orchestration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal abstract class RemoteSettingsServiceBase : BackgroundService, IProfilerSettingsService
{
    private readonly ILogger _logger;
    private readonly TaskCompletionSource<bool> _taskCompletionSource;
    private readonly UserConfigurationBase _userConfiguration;
    private readonly IProfilerFrontendClient _frontendClient;
    private readonly TimeSpan _frequency;
    private readonly bool _standaloneMode;
    private readonly bool _isDisabled;

    public SettingsContract? CurrentSettings { get; private set; } = null;

    public event Action<SettingsContract>? SettingsUpdated;

    public RemoteSettingsServiceBase(
        IProfilerFrontendClient frontendClient,
        IOptions<UserConfigurationBase> userConfigurationOptions,
        ILogger<RemoteSettingsServiceBase> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userConfiguration = userConfigurationOptions.Value ?? throw new ArgumentNullException(nameof(userConfigurationOptions));
        _frontendClient = frontendClient ?? throw new ArgumentNullException(nameof(frontendClient));
        _taskCompletionSource = new TaskCompletionSource<bool>();
        _frequency = _userConfiguration.ConfigurationUpdateFrequency;
        _standaloneMode = _userConfiguration.StandaloneMode;
        _isDisabled = _userConfiguration.IsDisabled;
    }

    public async Task<bool> WaitForInitializedAsync(TimeSpan timeout)
    {
        Task completed = await Task.WhenAny(Task.Delay(timeout), _taskCompletionSource.Task).ConfigureAwait(false);
        if (completed == _taskCompletionSource.Task)
        {
            // Initialize done.
            _logger.LogTrace("Initial remote settings fetched within given time: {timeout}.", timeout);
            return true;
        }
        else
        {
            _logger.LogDebug("Remote settings fetch timed out. Timeout settings: {timeout}", timeout);
            return false;
        }
    }

    protected abstract IDisposable EnterInternalZone();

    private async Task FetchRemoteSettingsAsync(CancellationToken cancellationToken)
    {
        using IDisposable internalZoneHandler = EnterInternalZone();
        try
        {
            _logger.LogTrace("Fetching remote settings.");
            SettingsContract newContract = await _frontendClient.GetProfilerSettingsAsync(cancellationToken).ConfigureAwait(false);
            if (newContract != null)
            {
                _logger.LogTrace("Remote settings fetched.");
                // New settings coming.
                OnSettingsUpdated(newContract);
            }
            else
            {
                _logger.LogTrace("No settings contract fetched.");
            }
        }
        catch (InstrumentationKeyInvalidException ikie)
        {
            _logger.LogError(ikie.Message);
            _logger.LogTrace(ikie.ToString());
        }
        catch (Exception ex) when (string.Equals(ex.Message, "Invalid instrumentation key format or instrumentation key has been revoked.", StringComparison.Ordinal))
        {
            _logger.LogError(ex.Message);
            _logger.LogTrace(ex.ToString());
        }
#pragma warning disable CA1031 // Only to allow for getting the value for the next iteration. The exception will be logged.
        catch (Exception ex)
#pragma warning restore CA1031 // Only to allow for getting the value for the next iteration. The exception will be logged.
        {
            // Move on for the next iteration.
            _logger.LogDebug("Unexpected error contacting service profiler service endpoint for settings. Details: {details}", ex);
            _logger.LogTrace("Error with trace: {error}", ex.ToString());
        }
        finally
        {
            // Either way, initialization is done.
            _taskCompletionSource.TrySetResult(true);
        }
    }

    private void OnSettingsUpdated(SettingsContract settingsContract)
    {
        CurrentSettings = settingsContract;
        SettingsUpdated?.Invoke(settingsContract);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (_standaloneMode)
        {
            _logger.LogTrace("Running in standalone mode. No remote settings will be fetched.");
            return;
        }

        if (_isDisabled)
        {
            _logger.LogTrace("Profiler is disabled. No remote settings will be fetched.");
            return;
        }

        if (_frequency > TimeSpan.Zero)
        {
            while (true)
            {
                await FetchRemoteSettingsAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(_frequency, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            _logger.LogWarning("Configuration update frequency can't be negative.");
        }
    }
}
