using System;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Core
{
    internal interface IThreadUtilities
    {
        Task CallWithTimeoutAsync(Action action, TimeSpan timeout = default);
    }
}
