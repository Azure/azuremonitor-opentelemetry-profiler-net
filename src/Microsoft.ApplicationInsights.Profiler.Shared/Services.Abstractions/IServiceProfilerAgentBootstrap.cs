using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IServiceProfilerAgentBootstrap
{
    /// <summary>
    /// Activates Application Insights Profiler Agent.
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken);
}