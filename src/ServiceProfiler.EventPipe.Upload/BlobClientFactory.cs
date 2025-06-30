using System;
using Azure.Storage.Blobs;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    public class BlobClientFactory : IBlobClientFactory
    {
        public BlobClient CreateBlobClient(Uri blobUriWithSASToken)
        {
            return new BlobClient(blobUriWithSASToken);
        }
    }
}