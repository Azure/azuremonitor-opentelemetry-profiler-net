using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Core;

internal sealed class ThreadUtilities : IThreadUtilities
{
    private ThreadUtilities() { }
    public static Lazy<ThreadUtilities> Instance { get; } = new Lazy<ThreadUtilities>(() => new ThreadUtilities(), LazyThreadSafetyMode.PublicationOnly);

    public async Task CallWithTimeoutAsync(Action action, TimeSpan timeout = default)
    {
        Task timeoutTask = Task.Delay(timeout);
        Task wrapper = Task.Run(action);

        if (timeout == default)
        {
            timeoutTask = Task.CompletedTask; // No timeout, just wait for the action to complete
        }

        Task completed = await Task.WhenAny(timeoutTask, wrapper).ConfigureAwait(false);

        // After Task.WhenAny, if the wrapper task has completed with an exception,
        // we need to await it to propagate the errors.
        // Otherwise exceptions from the action will be swallowed or unobserved.
        await completed.ConfigureAwait(false);

        if (completed == timeoutTask)
        {
            throw new TimeoutException("Call didn't finish in time.");
        }
    }
}
