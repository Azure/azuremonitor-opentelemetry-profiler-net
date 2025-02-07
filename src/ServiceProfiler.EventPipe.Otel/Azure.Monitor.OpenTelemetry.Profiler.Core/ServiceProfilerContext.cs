using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Contract;
using Microsoft.ServiceProfiler.Utilities;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class ServiceProfilerContext : IServiceProfilerContext
{
    private readonly IEndpointProvider _endpointProvider;
    private readonly IConnectionStringParserFactory _connectionStringParserFactory;
    private readonly ILogger _logger;
    private readonly ServiceProfilerOptions _options;

    public ServiceProfilerContext(
        IEndpointProvider endpointProvider,
        IConnectionStringParserFactory connectionStringParserFactory,
        IOptions<ServiceProfilerOptions> options,
        ILogger<ServiceProfilerContext> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _endpointProvider = endpointProvider ?? throw new ArgumentNullException(nameof(endpointProvider));

        ConnectionString = GetRequiredConnectionString(_options);
        _logger.LogDebug("Building {name}. Connection string: {connectionString}", nameof(ServiceProfilerContext), ConnectionString);

        _connectionStringParserFactory = connectionStringParserFactory ?? throw new ArgumentNullException(nameof(connectionStringParserFactory));
        IConnectionStringParser connectionStringParser = _connectionStringParserFactory.Create(ConnectionString);
        if (connectionStringParser.TryGetValue(ConnectionStringParser.Keys.InstrumentationKey, out string? instrumentationKeyValue))
        {
            _logger.LogDebug("Instrumentation key value: {iKey}", instrumentationKeyValue);
            AppInsightsInstrumentationKey = Guid.Parse(instrumentationKeyValue!);
        }
        else
        {
            _logger.LogError("Instrumentation key does not exist.");
        }

        StampFrontendEndpointUrl = _endpointProvider.GetEndpoint();
        if (StampFrontendEndpointUrl != new Uri(FrontendEndpoints.ProdGlobal, UriKind.Absolute))
        {
            _logger.LogWarning("Custom endpoint: {endpoint}. This is not supposed to be used in production. File an issue if this is not intended.", StampFrontendEndpointUrl);
        }
    }

    public Guid AppInsightsInstrumentationKey { get; }

    public bool HasAppInsightsInstrumentationKey => true;

    public string MachineName => EnvironmentUtilities.MachineName;

    public CancellationTokenSource ServiceProfilerCancellationTokenSource => new();

    public Uri StampFrontendEndpointUrl { get; }

    public string ConnectionString { get; }

    private string GetRequiredConnectionString(ServiceProfilerOptions options)
    {
        string? connectionString = options.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string is required. Please make sure its properly set.");
        }
        return connectionString;
    }
}