using System;
using Azure.Storage.Blobs;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    public interface IBlobClientFactory
    {
        BlobClient CreateBlobClient(Uri blobUriWithSASToken);
    }
}