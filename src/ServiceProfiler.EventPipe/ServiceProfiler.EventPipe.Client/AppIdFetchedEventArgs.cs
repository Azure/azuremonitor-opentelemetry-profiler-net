using System;

namespace Microsoft.ApplicationInsights.Profiler.Core
{
    internal sealed class AppIdFetchedEventArgs : EventArgs
    {
        public Guid AppId { get; }
        public AppIdFetchedEventArgs(Guid appId)
        {
            AppId = appId;
        }
    }
}