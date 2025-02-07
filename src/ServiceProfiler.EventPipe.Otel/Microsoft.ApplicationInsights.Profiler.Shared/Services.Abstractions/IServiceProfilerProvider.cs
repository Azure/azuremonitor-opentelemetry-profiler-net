using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceProfiler.Orchestration;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

public interface IServiceProfilerProvider
{
    Task<bool> StartServiceProfilerAsync(IProfilerSource source, CancellationToken cancellationToken = default);

    Task<bool> StopServiceProfilerAsync(IProfilerSource source, CancellationToken cancellationToken = default);
}
