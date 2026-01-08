using System;
using System.Threading;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal sealed class BootstrapState : IDisposable
{
    // To use Interlocked with bool, we use int where 0 = false, 1 = true
    private int _isDisposed = 0;

    private readonly ManualResetEventSlim _eventWaitHandle = new(initialState: false);
    private volatile bool _isProfilerRunning;

    /// <summary>
    /// Sets the profiler running state and signals any waiters.
    /// </summary>
    public void SetProfilerRunning(bool isRunning)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 0, 0) == 1)
        {
            throw new ObjectDisposedException(nameof(BootstrapState));
        }

        _isProfilerRunning = isRunning;
        _eventWaitHandle.Set();
    }

    /// <summary>
    /// Waits until bootstrap is completed and returns whether the profiler is running.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>>True if profiler is running; false otherwise.</returns>
    public bool IsProfilerRunning(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 0, 0) == 1)
        {
            throw new ObjectDisposedException(nameof(BootstrapState));
        }

        _eventWaitHandle.Wait(cancellationToken);
        return _isProfilerRunning;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1)
        {
            return;
        }

        _eventWaitHandle.Set(); // Unblock any waiters first
        _eventWaitHandle.Dispose();
    }
}