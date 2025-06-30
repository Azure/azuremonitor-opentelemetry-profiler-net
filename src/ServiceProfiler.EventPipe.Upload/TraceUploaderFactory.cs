using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ServiceProfiler.EventPipe.Upload;

internal class TraceUploaderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly UploadContext _uploadContext;

    public TraceUploaderFactory(
        IServiceProvider serviceProvider,
        UploadContext uploadContext)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _uploadContext = uploadContext ?? throw new ArgumentNullException(nameof(uploadContext));
    }

    public TraceUploader Create()
    {
        return _uploadContext.UseNamedPipe ?
            ActivatorUtilities.CreateInstance<TraceUploaderByNamedPipe>(_serviceProvider) :
            ActivatorUtilities.CreateInstance<TraceUploader>(_serviceProvider);
    }
}
