using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class MockBlobClient : BlobClient
    {
        private readonly Func<string, CancellationToken, Task<Response<BlobContentInfo>>> _onUploadAsync;
        private readonly Func<IDictionary<string, string>, BlobRequestConditions, CancellationToken, Task<Response<BlobInfo>>> _onSetMetadataAsync;

        internal IDictionary<string, string> Metadata { get; private set; }
        internal string LastUploadSource { get; private set; }

        public MockBlobClient(
            Uri uri,
            Func<string, CancellationToken, Task<Response<BlobContentInfo>>> onUploadAsync = null,
            Func<IDictionary<string, string>, BlobRequestConditions, CancellationToken, Task<Response<BlobInfo>>> onSetMetadataAsync = null)
            : base(uri)
        {
            _onUploadAsync = onUploadAsync ?? ((path, cancellationToken) =>
            {
                LastUploadSource = path;
                return Task.FromResult<Response<BlobContentInfo>>(null);
            });
            _onSetMetadataAsync = onSetMetadataAsync ?? ((metadata, conditions, cancellationToken) =>
            {
                Metadata = metadata;
                return Task.FromResult<Response<BlobInfo>>(null);
            });
        }

        public override Task<Response<BlobContentInfo>> UploadAsync(string path, CancellationToken cancellationToken)
        {
            return _onUploadAsync?.Invoke(path, cancellationToken);
        }

        public override Task<Response<BlobInfo>> SetMetadataAsync(IDictionary<string, string> metadata, BlobRequestConditions conditions = null, CancellationToken cancellationToken = default)
        {
            return _onSetMetadataAsync?.Invoke(metadata, conditions, cancellationToken);
        }
    }
}