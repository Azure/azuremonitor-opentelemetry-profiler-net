using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    /// <summary>
    /// A thin wrapper responsible for serialize and deserialize a series of sample activities.
    /// </summary>
    class SampleActivitySerializer : ISampleActivitySerializer
    {
        private readonly ISerializationProvider _serializationProvider;

        public SampleActivitySerializer(ISerializationProvider serializationProvider)
        {
            _serializationProvider = serializationProvider;
        }

        public async Task SerializeToFileAsync(IEnumerable<SampleActivity> samples, string destinationFilePath)
        {
            if (_serializationProvider.TrySerialize(samples, out string? serialized))
            {
                using StreamWriter file = File.CreateText(destinationFilePath);
                await file.WriteAsync(serialized).ConfigureAwait(false);
            }
        }

        public async Task<IEnumerable<SampleActivity>> DeserializeFromFileAsync(string sourceFilePath)
        {
            using StreamReader file = File.OpenText(sourceFilePath);
            string stringContent = await file.ReadToEndAsync().ConfigureAwait(false);
            if(_serializationProvider.TryDeserialize<IEnumerable<SampleActivity>>(stringContent, out IEnumerable<SampleActivity>? result))
            {
                return result!;
            }

            return Enumerable.Empty<SampleActivity>();
        }
    }
}
