using Microsoft.ApplicationInsights.Profiler.Uploader;

namespace ServiceProfiler.EventPipe.Upload;

/// <summary>
/// Responsible for sending
/// </summary>
internal interface ICustomEventsSender
{
    public void Send(UploadContextExtension context);
}
