using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore
{
    /// <summary>
    /// A bootstrap to use when profiler is disabled.
    /// This is introduced to prevent dependency injection container to create any
    /// service profiler related dependencies of
    /// <see cref="Microsoft.ApplicationInsights.Profiler.AspNetCore.ServiceProfilerAgentBootstrap" />.
    /// </summary>
    internal class DisabledAgentBootstrap : IServiceProfilerAgentBootstrap
    {
        private readonly ILogger _logger;

        public DisabledAgentBootstrap(ILogger<DisabledAgentBootstrap> logger)
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public Task ActivateAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Service Profiler is disabled by user configuration.");
            return Task.CompletedTask;
        }
    }
}
