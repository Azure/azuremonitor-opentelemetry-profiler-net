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
        _connectionStringParserFactory = connectionStringParserFactory ?? throw new ArgumentNullException(nameof(connectionStringParserFactory));
        ConnectionString = _options.ConnectionString;
        _logger.LogDebug("Building {name}. Connection string: {connectionString}", nameof(ServiceProfilerContext), ConnectionString);

        IConnectionStringParser connectionStringParser = _connectionStringParserFactory.Create(ConnectionString!);
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

    // public Guid AppInsightsAppId { get; private set; }

    public Guid AppInsightsInstrumentationKey { get; }

    public bool HasAppInsightsInstrumentationKey => true;

    public string MachineName => EnvironmentUtilities.MachineName;

    public CancellationTokenSource ServiceProfilerCancellationTokenSource => new();

    public Uri StampFrontendEndpointUrl { get; }

    public string? ConnectionString { get; }

    // public event EventHandler<AppIdFetchedEventArgs>? AppIdFetched;

    // public Task<Guid> GetAppInsightsAppIdAsync()
    // {
    //     return GetAppInsightsAppIdAsync(cancellationToken: default);
    // }

    // public async Task<Guid> GetAppInsightsAppIdAsync(CancellationToken cancellationToken)
    // {
    //     if (AppInsightsAppId != default)
    //     {
    //         return AppInsightsAppId;
    //     }

    //     try
    //     {
    //         // Fetch & return.
    //         cancellationToken.ThrowIfCancellationRequested();
    //         AppInsightsProfile appInsightsProfile = await _appInsightsProfileFetcher.FetchProfileAsync(AppInsightsInstrumentationKey, retryCount: 5).ConfigureAwait(false);

    //         cancellationToken.ThrowIfCancellationRequested();
    //         Guid appId = appInsightsProfile.AppId;
    //         AppInsightsAppId = appId;
    //         OnAppIdFetched(appId);

    //         return appId;
    //     }
    //     catch (InstrumentationKeyInvalidException ikie)
    //     {
    //         _logger.LogError(ikie, "Profiler Instrumentation Key is invalid.");
    //         throw;
    //     }
    // }

    // public void OnAppIdFetched(Guid appId)
    // {
    //     AppIdFetched?.Invoke(this, new AppIdFetchedEventArgs(appId));
    // }
}