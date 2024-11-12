using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Azure.Monitor.OpenTelemetry.AspNetCore;

internal class DisabledAgentBootstrap(ILogger<DisabledAgentBootstrap> logger) : IServiceProfilerAgentBootstrap
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Task ActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service Profiler is disabled by user configuration.");
        return Task.CompletedTask;
    }
}