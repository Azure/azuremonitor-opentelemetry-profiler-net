using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal interface ITraceUploader
    {
        Task UploadAsync(CancellationToken cancellationToken);
    }
}
