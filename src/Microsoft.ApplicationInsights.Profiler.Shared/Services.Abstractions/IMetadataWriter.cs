using Microsoft.ServiceProfiler.Contract;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IMetadataWriter
{
    Task WriteAsync(string filePath, IEnumerable<ArtifactLocationProperties> locationProperties, CancellationToken cancellationToken);
}
