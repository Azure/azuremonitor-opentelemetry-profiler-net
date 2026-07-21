using Azure.Monitor.Diagnostics.Profiler;
using Azure.Core;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Auth;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Orchestration;
using Microsoft.ApplicationInsights.Profiler.Core.TraceControls;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.RoleNames;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.TraceScavenger;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.DataContract.Settings;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Orchestration.MetricsProviders;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.Common.Utilities;
using ServiceProfiler.EventPipe.Client;
using ServiceProfiler.EventPipe.Logging;
using System;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Microsoft.ApplicationInsights.Profiler.Core;

/// <summary>
/// Service Collection extensions for the Core project.
/// </summary>
/// <remarks>
/// Many services within the core have been registered in the header projects, such as Microsoft.ApplicationInsights.Profiler.AspNetCore.
/// Please consider migrating them back to this project to enhance the clarity of the architecture.
/// <remarks>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register services provided in this core library.
    /// </summary>
    /// <param name="services"></param>
    public static IServiceCollection AddProfilerCoreServices(this IServiceCollection services)
    {
        if (!PlatformSupport.IsSupportedPlatform())
        {
            return services;
        }

        // User Connection String
        services.AddSingleton<ConnectionString>(p =>
        {
            TelemetryConfiguration telemetryConfiguration = p.GetRequiredService<TelemetryConfiguration>();
            string connectionStringValue = telemetryConfiguration.ConnectionString;
            if (!ConnectionString.TryParse(connectionStringValue, out ConnectionString instance))
            {
                ILogger logger = p.GetRequiredService<ILogger<ConnectionString>>();
                logger.LogDebug("Connection string is not set or is invalid. Connection string provided: {connectionString}", connectionStringValue);
            }

            return instance;
        });

        // Utilities
        services.AddTransient<IEventPipeEnvironmentCheckService, EventPipeEnvironmentCheckService>();

        services.AddSingleton<IFile, SystemFile>();
        services.AddSingleton<IEnvironment, SystemEnvironment>();
        services.AddSingleton<IZipFile, SystemZipFile>();
        services.AddTransient<IRandomSource, DefaultRandomSource>();
        services.AddTransient<IDelaySource, DefaultDelaySource>();

        services.AddSingleton<IProfilerCoreAssemblyInfo>(_ => ProfilerCoreAssemblyInfo.Instance);
        services.AddTransient<IUserCacheManager, UserCacheManager>();
        services.AddSingleton<ITraceFileFormatDefinition>(_ => CurrentTraceFileFormat.Instance);

        // Telemetry - TODO:
        // 1. Have a dedicated application insights resource
        // 2. Support multiple endpoints / multiple clouds

        // AI for Microsoft depends on user settings.
        services.AddSingleton<IAppInsightsLogger>(provider =>
        {
            UserConfiguration userConfiguration = provider.GetRequiredService<IOptions<UserConfiguration>>().Value;
            ILogger logger = provider.GetRequiredService<ILogger<IAppInsightsLogger>>();

            if (userConfiguration.ProvideAnonymousTelemetry)
            {
                logger.LogDebug("Sending anonymous telemetry data to Microsoft to make this product better.");
                return new EventPipeAppInsightsLogger(
                    TelemetryConstants.ServiceProfilerAgentConnectionString);
            }
            else
            {
                logger.LogDebug("No anonymous telemetry data is sent to Microsoft.");
                return new NullAppInsightsLogger();
            }
        });

        // AI for the customer.
        // Build the customer telemetry client from the full connection string (so the regional
        // IngestionEndpoint is honored) and the configured AAD token credential (so it works when
        // local auth is disabled). This is the same mechanism used by AgentStatusSender.
        // Register it once as a dedicated holder so the *same* instance can be (a) part of the
        // IAppInsightsLogger fan-out used for notable Start/Stop traces, and (b) targeted directly
        // (customer-only) when forwarding uploader logs - which must never go to Microsoft's
        // anonymous-telemetry resource.
        services.AddSingleton(sp => new CustomerAppInsightsLogger(CreateCustomerAppInsightsLogger(sp)));
        services.AddSingleton<IAppInsightsLogger>(sp => sp.GetRequiredService<CustomerAppInsightsLogger>().Logger);

        // Heartbeats
        services.TryAddSingleton<IEventPipeTelemetryTracker, TelemetryTracker>();
        services.AddHostedService<TelemetryTrackerBackgroundService>();

        // Consume IEnumerable<IAppInsightsLogger> to form a sink.
        services.TryAddSingleton<IAppInsightsSinks, AppInsightsSinks>();

        // Agent status
        services.AddSingleton<IAgentStatusSender, AgentStatusSender>();
        services.AddSingleton<IAgentStatusService, AgentStatusService>();

        // Role name detectors and sources
        services.AddSingleton<IRoleNameDetector, EnvRoleNameDetector>(_ => new EnvRoleNameDetector("WEBSITE_SITE_NAME"));
        services.AddSingleton<IRoleNameDetector, EnvRoleNameDetector>(_ => new EnvRoleNameDetector("RoleName"));
        services.AddSingleton<IRoleNameDetector, UnknownRoleNameDetector>();
        services.AddSingleton<IRoleNameSource, AggregatedRoleNameSource>();

        // Role instance detectors and sources
        services.AddSingleton<IRoleInstanceDetector, ServiceProfilerContextRoleInstanceDetector>();
        services.AddSingleton<IRoleInstanceSource, AggregatedRoleInstanceSource>();

        // Application version detectors and source
        services.AddSingleton<IAppVersionDetector, AssemblyAppVersionDetector>();
        services.AddSingleton<IAppVersionSource, AggregatedAppVersionSource>();

        // Profiler
        services.AddSingleton<SampleActivityContainerFactory>();
        services.AddSingleton<ITraceSessionListenerFactory, TraceSessionListenerFactory>();

        services.AddTransient<ICustomEventsBuilder, CustomEventsBuilder>();
        services.AddSingleton<IPostStopProcessorFactory, PostStopProcessorFactory>();
        services.AddSingleton<IServiceProfilerProvider, ServiceProfilerProvider>();
        services.AddSingleton<DiagnosticsClientTraceConfiguration>();

        services.AddSingleton(_ => DiagnosticsClientProvider.Instance);

        // Named pipe client
        services.AddSingleton<IPayloadSerializer, HighPerfJsonSerializationProvider>();
        services.AddSingleton<ISerializationProvider, HighPerfJsonSerializationProvider>();
        services.AddSingleton<ISerializationOptionsProvider<JsonSerializerOptions>, HighPerfJsonSerializationProvider>();

        // Profiler context
        services.AddSingleton(static p =>
        {
            ConnectionString? connectionString = p.GetService<ConnectionString>();

            // If connection string is null, return null context.
            string breezeEndpoint;
            if (connectionString is null)
            {
                // Use a placeholder endpoint to avoid null reference exceptions downstream.
                breezeEndpoint = "https://placeholder.endpoint/";
            }
            else
            {
                breezeEndpoint = ConnectionStringFeatures.GetIngestionEndpoint(connectionString).AbsoluteUri;
            }

            IEndpointProvider endpointProvider = p.GetRequiredService<IEndpointProvider>();
            return new AppInsightsProfileFetcher(breezeEndpoint: breezeEndpoint);
        });

        services.TryAddSingleton<IEndpointProvider, EndpointProviderMirror>();

        services.AddSingleton<IMetadataWriter, MetadataWriter>();
        services.AddSingleton<IServiceProfilerContext>(p =>
        {
            TelemetryConfiguration telemetryConfiguration = p.GetRequiredService<TelemetryConfiguration>();
            return new ServiceProfilerContext(
                p.GetService<ConnectionString>(),
                telemetryConfiguration.ConnectionString,
                p.GetRequiredService<IEndpointProvider>(),
                p.GetRequiredService<ILogger<ServiceProfilerContext>>());
        });

        services.AddSingleton<INamedPipeClientFactory, NamedPipeClientFactory>();

        // Compatibility test
        bool isRunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        if (isRunningOnWindows)
        {
            services.AddTransient<INetCoreAppVersion, WindowsNetCoreAppVersion>();
        }
        else
        {
            services.AddTransient<INetCoreAppVersion, LinuxNetCoreAppVersion>();
        }
        services.AddTransient<IVersionProvider>(p => ActivatorUtilities.CreateInstance<VersionProvider>(p, RuntimeInformation.FrameworkDescription));
        services.AddSingleton<ICompatibilityUtilityFactory, RuntimeCompatibilityUtilityFactory>();
        // ~

        // Customizations
        services.AddSingleton<ProfilerSettings>();
        services.AddSingleton<IProfilerSettingsService>(p =>
        {
            UserConfiguration userConfiguration = p.GetRequiredService<IOptions<UserConfiguration>>().Value;
            if (userConfiguration.StandaloneMode)
            {
                return ActivatorUtilities.CreateInstance<LocalProfileSettingsService>(p);
            }
            else
            {
                return ActivatorUtilities.CreateInstance<RemoteProfilerSettingsService>(p);
            }
        });

        services.AddHostedService(p =>
        {
            BackgroundService backgroundService = (BackgroundService)p.GetRequiredService<IProfilerSettingsService>();
            return backgroundService;
        });

        // Triggers
        services.AddSingleton(_ => SettingsParser.Instance);
        services.AddSingleton<CpuTriggerSettings>();
        services.AddTransient<MemoryTriggerSettings>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddKeyedSingleton<IMetricsProvider, ProcessInfoCPUMetricsProvider>(MetricsProviderCategory.CPU);
            services.AddKeyedSingleton<IMetricsProvider, WindowsMemoryMetricsProvider>(MetricsProviderCategory.Memory);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddKeyedSingleton<IMetricsProvider, CgroupCpuMetricsProvider>(MetricsProviderCategory.CPU);
            services.AddSingleton<MemInfoItemParser>();
            services.AddSingleton<IMemInfoReader, ProcMemInfoReader>();
            services.AddKeyedSingleton<IMetricsProvider, MemInfoFileMemoryMetricsProvider>(MetricsProviderCategory.Memory);
        }

        services.AddSingleton<IResourceUsageSource, ResourceUsageSource>();

        return services
            .AddFrontendClient()
            .AddSchedulers()
            .AddAppInsightsAADAuthServices()
            .AddUploaderCallerServices()
            .AddTraceScavengerServices()
            .AddTraceControl();
    }

    /// <summary>
    /// Creates the customer-facing Application Insights logger used for the profiler's notable
    /// Start/Stop traces. Unlike the Microsoft anonymous-telemetry logger, this uses the full
    /// connection string (so the regional IngestionEndpoint is honored) and the configured AAD
    /// token credential (so it works when local auth is disabled).
    /// </summary>
    private static IAppInsightsLogger CreateCustomerAppInsightsLogger(IServiceProvider provider)
    {
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        ILogger logger = loggerFactory.CreateLogger("Microsoft.ApplicationInsights.Profiler.Core.Logging.CustomerAppInsightsLogger");

        ConnectionString? connectionString = provider.GetRequiredService<IServiceProfilerContext>().ConnectionString;
        if (connectionString is null)
        {
            logger.LogDebug("No Application Insights connection string is available. Customer notable-trace telemetry is disabled.");
            return new NullAppInsightsLogger();
        }

        IAuthTokenProvider authTokenProvider = provider.GetRequiredService<IAuthTokenProvider>();
        TokenCredential? tokenCredential = authTokenProvider.IsAADAuthenticateEnabled
            ? new AADAuthTokenCredential(authTokenProvider, loggerFactory.CreateLogger<AADAuthTokenCredential>())
            : null;

        // Hardening: surface a warning when there is no ingestion endpoint to route to. Without it,
        // telemetry falls back to the global endpoint and is silently dropped by workspace-based resources.
        if (connectionString.GetFeatureEndpoint("Ingestion") is null)
        {
            logger.LogWarning(
                "The Application Insights connection string does not specify an IngestionEndpoint. Profiler Start/Stop traces are " +
                "sent to the global ingestion endpoint and may be dropped by workspace-based resources. Provide a full connection " +
                "string (including IngestionEndpoint) to ensure these traces are ingested.");
        }

        TelemetryConfiguration telemetryConfiguration = new TelemetryConfigurationBuilder(
            connectionString,
            tokenCredential,
            telemetryInitializers: [],
            loggerFactory.CreateLogger<TelemetryConfigurationBuilder>()).Build();

        return new EventPipeAppInsightsLogger(telemetryConfiguration);
    }

    // Register services related to Application Insights AAD Auth
    private static IServiceCollection AddAppInsightsAADAuthServices(this IServiceCollection services)
    {
        services.TryAddSingleton<IAccessTokenFactory, AccessTokenFactory>();
        services.TryAddSingleton<IAuthTokenProvider, AuthTokenProvider>();
        return services;
    }

    private static IServiceCollection AddUploaderCallerServices(this IServiceCollection services)
    {
        services.AddTransient<IUploadContextValidator, UploadContextValidator>();

        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorByEnvironmentVariable>();
        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorInUserCache>();
        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorByUnzipping>();
        services.AddTransient<IUploaderPathProvider, UploaderPathProvider>();

        services.AddSingleton<IOutOfProcCallerFactory, OutOfProcCallerFactory>();
        services.AddSingleton<SubprocessLogForwarder>();
        // Classic profiler: the ILogger pipeline is not wired to the customer's Application
        // Insights, so forward uploader logs through the profiler's dedicated telemetry channel.
        services.AddSingleton<IUploaderLogForwarderSink, UploaderLogForwarderSink>();

        services.AddTransient<ITraceUploader, TraceUploaderProxy>();

        return services;
    }

    private static IServiceCollection AddTraceScavengerServices(this IServiceCollection services)
    {
        // Register FileScavenger
        services.AddSingleton<IFileScavengerEventListener, TraceScavengerListener>();

        services.AddSingleton(CreateFileScavenger);

        services.AddHostedService<TraceScavengerService>();

        return services;
    }

    private static FileScavenger CreateFileScavenger(IServiceProvider serviceProvider)
    {
        UserConfiguration configuration = serviceProvider.GetRequiredService<IOptions<UserConfiguration>>().Value;
        IUserCacheManager cacheManager = serviceProvider.GetRequiredService<IUserCacheManager>();
        ITraceFileFormatDefinition traceFileFormatDefinition = serviceProvider.GetRequiredService<ITraceFileFormatDefinition>();

        return ActivatorUtilities.CreateInstance<FileScavenger>(serviceProvider,
            new FileScavengerOptions(cacheManager.TempTraceDirectory.FullName)
            {
                DeletePattern = "*" + traceFileFormatDefinition.FileExtension, // *.netperf
                GracePeriod = configuration.TraceScavenger.GracePeriod,
            });
    }

    private static IServiceCollection AddSchedulers(this IServiceCollection services)
    {
        services.AddSingleton<ProcessExpirationPolicy>();
        services.AddSingleton<LimitedExpirationPolicyFactory>();

        services.AddSingleton<IOrchestrator, OrchestrationImp>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, OneTimeSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, RandomSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, OnDemandSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, MemoryMonitoringSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, CPUMonitoringSchedulingPolicy>());

        return services;
    }

    private static IServiceCollection AddFrontendClient(this IServiceCollection services)
    {
        services.AddSingleton<IProfilerClient>(p => ActivatorUtilities.CreateInstance<ProfilerClientFactory>(p).CreateProfilerClient());

        // Concurrency control (lease-based). No-op in standalone mode (no backend to lease from).
        services.AddSingleton<IProfilerLeaseClient, DiagnosticsProfilerLeaseClient>();
        services.AddSingleton<IProfilerConcurrencyControlClient>(p =>
        {
            UserConfiguration options = p.GetRequiredService<IOptions<UserConfiguration>>().Value;
            if (options.StandaloneMode)
            {
                return new NoOpProfilerConcurrencyControlClient();
            }

            return ActivatorUtilities.CreateInstance<ProfilerConcurrencyControlClient>(p);
        });

        return services;
    }

    /// <summary>
    /// Registers services to control profile sessions.
    /// </summary>
    private static IServiceCollection AddTraceControl(this IServiceCollection services)
    {
        // Dependency
        services.TryAddSingleton<IThreadUtilities>(p => ThreadUtilities.Instance.Value);
        // Trace control
        services.TryAddSingleton<ITraceControl, DiagnosticsClientTraceControl>();

        return services;
    }
}
