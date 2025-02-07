using Microsoft.Extensions.DependencyInjection;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;

internal class TraceSessionListenerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public TraceSessionListenerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public TraceSessionListener Create() 
        => ActivatorUtilities.CreateInstance<TraceSessionListener>(_serviceProvider);
}