using Microsoft.ServiceProfiler.Agent;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    /// <summary>
    /// Service that can create <see cref="IAppProfileClient"/> instance.
    /// </summary>
    internal interface IAppProfileClientFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="IAppProfileClient"/>.
        /// </summary>
        IAppProfileClient Create(UploadContextExtension uploadContext);
    }
}
