using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ServiceProfiler.Contract;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class MetadataWriter : IMetadataWriter
{
    public async Task WriteAsync(string filePath, IEnumerable<ArtifactLocationProperties> locationProperties, CancellationToken cancellationToken)
    {
        using StreamWriter writer = new(filePath, true, Encoding.UTF8);

        if (locationProperties != null && locationProperties.Any())
        {
            foreach (ArtifactLocationProperties line in locationProperties)
            {
                await writer.WriteLineAsync(line.ToString()).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        else
        {
            await writer.WriteLineAsync("No metadata.").ConfigureAwait(false);
        }
    }
}
