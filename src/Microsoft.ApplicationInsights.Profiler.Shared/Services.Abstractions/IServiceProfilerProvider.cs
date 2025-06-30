using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceProfiler.Orchestration;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IServiceProfilerProvider
{
    /// <summary>
    /// Starts the service profiler for the given source.
    /// </summary>
    /// <returns>Returns true when the profiler started succeeded. Otherwise, false.</returns>
    Task<bool> StartServiceProfilerAsync(IProfilerSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the service profiler for the given source.
    /// </summary>
    /// <returns>Returns true when the profiler is stopped. Otherwise, false.</returns>
    /// <remarks>
    /// The return value indicates whether the profiler was successfully stopped.
    /// <remarks>
    Task<bool> StopServiceProfilerAsync(IProfilerSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets if the profiler is currently running.
    /// </summary>
    public bool IsProfilerRunning { get; }
}
