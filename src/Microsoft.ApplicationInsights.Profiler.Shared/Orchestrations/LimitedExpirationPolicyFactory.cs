using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceProfiler.Orchestration;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;

internal class LimitedExpirationPolicyFactory(IServiceProvider serviceProvider)
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public LimitedExpirationPolicy Create(int count)
    {
        return ActivatorUtilities.CreateInstance<LimitedExpirationPolicy>(_serviceProvider, count);
    }
}