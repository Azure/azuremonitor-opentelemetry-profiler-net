using Microsoft.ServiceProfiler.Agent.FrontendClient;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal interface IProfilerFrontendClientBuilder
    {
        IProfilerFrontendClientBuilder WithUploadContext(UploadContextExtension uploadContext);
        IProfilerFrontendClient Build();
    }
}

