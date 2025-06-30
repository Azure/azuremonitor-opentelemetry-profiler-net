using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.RoleNames;

internal class ServiceProfilerContextRoleInstanceDetector : IRoleInstanceDetector
{
    private readonly IServiceProfilerContext _serviceProfilerContext;

    public ServiceProfilerContextRoleInstanceDetector(IServiceProfilerContext serviceProfilerContext)
    {
        _serviceProfilerContext = serviceProfilerContext ?? throw new System.ArgumentNullException(nameof(serviceProfilerContext));
    }

    public string? GetRoleInstance()
    {
        return _serviceProfilerContext.MachineName;
    }
}