using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class RuntimeCompatibilityUtilityFactory(IServiceProvider serviceProvider) : ICompatibilityUtilityFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public ICompatibilityUtility Create() => ActivatorUtilities.CreateInstance<RuntimeCompatibilityUtility>(_serviceProvider);
}