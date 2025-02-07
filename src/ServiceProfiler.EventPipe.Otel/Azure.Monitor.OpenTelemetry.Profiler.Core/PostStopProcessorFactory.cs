using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class PostStopProcessorFactory(IServiceProvider serviceProvider) : IPostStopProcessorFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public IPostStopProcessor Create() => ActivatorUtilities.CreateInstance<PostStopProcessor>(_serviceProvider);
}