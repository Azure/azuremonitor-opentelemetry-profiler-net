using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore;

internal class ServiceProfilerBackgroundService : BackgroundService
{
    private readonly IServiceProfilerAgentBootstrap _bootstrap;

    public ServiceProfilerBackgroundService(IServiceProfilerAgentBootstrap bootstrap)
    {
        _bootstrap = bootstrap ?? throw new System.ArgumentNullException(nameof(bootstrap));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Reduce chance to block the startup of the host. Refer to https://github.com/dotnet/runtime/issues/36063 for more details.
        await Task.Yield();

        await _bootstrap.ActivateAsync(stoppingToken).ConfigureAwait(false);
    }
}
