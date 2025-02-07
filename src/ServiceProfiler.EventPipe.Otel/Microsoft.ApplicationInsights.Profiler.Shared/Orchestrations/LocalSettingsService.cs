using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;
using Microsoft.ServiceProfiler.Orchestration;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

/// <summary>
/// Settings service used in standalone mode.
/// </summary>
internal sealed class LocalProfileSettingsService : BackgroundService, IProfilerSettingsService
{
    private readonly ILogger _logger;

    public SettingsContract? CurrentSettings { get; }
    public event Action<SettingsContract?>? SettingsUpdated;

    public LocalProfileSettingsService(
        ILogger<LocalProfileSettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // TODO: Support overwrite trigger settings in standalone mode.
        _logger.LogInformation("Getting remote settings in standalone mode. Returns null.");
        CurrentSettings = null;
        SettingsUpdated?.Invoke(CurrentSettings);
        _logger.LogTrace("{serviceName} is initialized.", nameof(LocalProfileSettingsService));
    }

    public Task<bool> WaitForInitializedAsync(TimeSpan timeout)
    {
        // Task complete as long as the constructor is executed.
        return Task.FromResult(true);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
}
