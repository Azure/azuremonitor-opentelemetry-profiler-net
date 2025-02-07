using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal class OutOfProcCallerFactory : IOutOfProcCallerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public OutOfProcCallerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public IOutOfProcCaller Create(string executable, string arguments)
    {
        return ActivatorUtilities.CreateInstance<OutOfProcCaller>(_serviceProvider, executable, arguments);
    }
}
