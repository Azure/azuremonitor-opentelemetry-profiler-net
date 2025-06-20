using System;
using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal class UploadContextExtension
    {
        public UploadContextExtension(UploadContext uploadContext)
        {
            UploadContext = uploadContext ?? throw new ArgumentNullException(nameof(uploadContext));
        }

        public UploadContext UploadContext { get; }
        public Guid VerifiedAppId { get; set; }


        public IPCAdditionalData? AdditionalData { get; set; }

        public TokenCredential? TokenCredential { get; set; } = null;
    }
}
