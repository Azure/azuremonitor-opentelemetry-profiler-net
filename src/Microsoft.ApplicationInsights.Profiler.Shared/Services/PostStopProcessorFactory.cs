using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class PostStopProcessorFactory(IServiceProvider serviceProvider) : IPostStopProcessorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public IPostStopProcessor Create() => ActivatorUtilities.CreateInstance<PostStopProcessor>(_serviceProvider);
}