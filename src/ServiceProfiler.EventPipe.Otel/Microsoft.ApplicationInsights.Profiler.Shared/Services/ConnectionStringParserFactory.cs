using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class ConnectionStringParserFactory(IServiceProvider serviceProvider) : IConnectionStringParserFactory
{
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public IConnectionStringParser Create(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Can't create a connection string parser without a connection string. Please make sure the connection string is properly set.");
        }

        return ActivatorUtilities.CreateInstance<ConnectionStringParser>(provider: _serviceProvider, connectionString);
    }
}
