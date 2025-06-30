using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    // Keep for backward compatibility, still used in the uploader.
    internal interface ISampleActivitySerializer
    {
        /// <summary>
        /// Deserialize an enumerable of samples from given file.
        /// </summary>
        Task<IEnumerable<SampleActivity>> DeserializeFromFileAsync(string sourceFilePath);

        /// <summary>
        /// Serialize a series of samples to a file.
        /// </summary>
        Task SerializeToFileAsync(IEnumerable<SampleActivity> samples, string destinationFilePath);
    }
}
