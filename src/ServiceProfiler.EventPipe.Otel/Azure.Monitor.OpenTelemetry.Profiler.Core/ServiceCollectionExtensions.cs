//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners;
using Azure.Monitor.OpenTelemetry.Profiler.Core.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.TraceScavenger;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.DataContract.Settings;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Orchestration.MetricsProviders;
using Microsoft.ServiceProfiler.Utilities;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServiceProfilerCore(this IServiceCollection services)
    {
        // Utilities
        services.AddSingleton<IConnectionStringParserFactory, ConnectionStringParserFactory>();
        services.AddSingleton<IRoleNameSource, AggregatedRoleNameSource>();


        services.AddSingleton<IFile, SystemFile>();
        services.AddSingleton<IEnvironment, SystemEnvironment>();
        services.AddSingleton<IZipFile, SystemZipFile>();
        services.AddSingleton<IDelaySource, DefaultDelaySource>();
        services.AddSingleton<IRandomSource, DefaultRandomSource>();

        services.AddSingleton<IProfilerCoreAssemblyInfo>(_ => ProfilerCoreAssemblyInfo.Instance);
        services.AddSingleton<IUserCacheManager, UserCacheManager>();

        services.AddSingleton<IPayloadSerializer, HighPerfJsonSerializationProvider>();
        services.AddSingleton<ISerializationProvider, HighPerfJsonSerializationProvider>();
        services.AddSingleton<ISerializationOptionsProvider<JsonSerializerOptions>, HighPerfJsonSerializationProvider>();
        services.AddSingleton<ISerializationProvider, HighPerfJsonSerializationProvider>();

        // Uploader caller
        AddUploaderCallerServices(services);

        // Named pipe client
        services.AddSingleton<INamedPipeClientFactory, NamedPipeClientFactory>();

        // Profiler Context
        services.AddSingleton<IEndpointProvider, EndpointProvider>();
        services.AddTransient<IMetadataWriter, MetadataWriter>();

        // Transient trace session listeners
        services.AddTransient<SampleActivityContainer>();
        services.AddTransient<SampleCollector>();
        services.AddSingleton<TraceSessionListenerFactory>();

        // Profiler
        services.AddTransient<ICustomEventsBuilder, CustomEventsBuilder>();
        services.AddSingleton<IPostStopProcessorFactory, PostStopProcessorFactory>();

        services.AddSingleton(_ => DiagnosticsClientProvider.Instance);
        services.AddSingleton<DiagnosticsClientTraceConfiguration>();
        services.AddSingleton<ITraceControl, DiagnosticsClientTrace>();
        services.AddSingleton<IServiceProfilerContext, ServiceProfilerContext>();
        services.AddSingleton<IServiceProfilerProvider, OpenTelemetryProfilerProvider>();

        // Client
        services.AddSingleton(p => ActivatorUtilities.CreateInstance<ProfilerFrontendClientFactory>(p).CreateProfilerFrontendClient());

        // Token
        services.AddSingleton<IAuthTokenProvider, AuthTokenProvider>();

        // Orchestrator
        AddSchedulers(services);

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
            ServiceProfilerOptions userConfiguration = p.GetRequiredService<IOptions<ServiceProfilerOptions>>().Value;

            if (userConfiguration.StandaloneMode)
            {
                return ActivatorUtilities.CreateInstance<LocalProfileSettingsService>(p);
            }
            else
            {
                return ActivatorUtilities.CreateInstance<RemoteSettingsService>(p);
            }
        });

        services.AddHostedService(p =>
        {
            BackgroundService? backgroundService = p.GetRequiredService<IProfilerSettingsService>() as BackgroundService;
            return backgroundService ?? throw new InvalidOperationException($"The {nameof(IProfilerSettingsService)} is required to be a background service.");
        });

        // Triggers
        services.AddSingleton(_ => SettingsParser.Instance);
        services.AddSingleton<CpuTriggerSettings>();
        services.AddSingleton<MemoryTriggerSettings>();

        services.AddKeyedSingleton<IMetricsProvider, ProcessInfoCPUMetricsProvider>(MetricsProviderCategory.CPU);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            services.AddKeyedSingleton<IMetricsProvider, WindowsMemoryMetricsProvider>(MetricsProviderCategory.Memory);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            services.AddSingleton<MemInfoItemParser>();
            services.AddSingleton<IMemInfoReader, ProcMemInfoReader>();
            services.AddKeyedSingleton<IMetricsProvider, MemInfoFileMemoryMetricsProvider>(MetricsProviderCategory.Memory);
        }
        else
        {
            throw new NotSupportedException($"Only support {OSPlatform.Windows} and {OSPlatform.Linux}.");
        }

        services.AddSingleton<IResourceUsageSource, ResourceUsageSource>();

        // Scavengers
        AddTraceScavengerServices(services);

        return services;
    }

    private static void AddUploaderCallerServices(IServiceCollection services)
    {
        services.AddTransient<IUploadContextValidator, UploadContextValidator>();

        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorByEnvironmentVariable>();
        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorInUserCache>();
        services.AddTransient<IPrioritizedUploaderLocator, UploaderLocatorByUnzipping>();
        services.AddTransient<IUploaderPathProvider, UploaderPathProvider>();

        services.AddSingleton<IOutOfProcCallerFactory, OutOfProcCallerFactory>();

        services.AddTransient<ITraceUploader, TraceUploaderProxy>();
    }

    private static void AddSchedulers(IServiceCollection services)
    {
        services.AddSingleton<ProcessExpirationPolicy>();
        services.AddSingleton<LimitedExpirationPolicyFactory>();

        services.AddSingleton<IOrchestrator, OrchestrationImp>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, OneTimeSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, RandomSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, OnDemandSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, MemoryMonitoringSchedulingPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<SchedulingPolicy, CPUMonitoringSchedulingPolicy>());
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
        ServiceProfilerOptions configuration = serviceProvider.GetRequiredService<IOptions<ServiceProfilerOptions>>().Value;
        IUserCacheManager cacheManager = serviceProvider.GetRequiredService<IUserCacheManager>();

        return ActivatorUtilities.CreateInstance<FileScavenger>(serviceProvider,
            new FileScavengerOptions(cacheManager.TempTraceDirectory.FullName)
            {
                DeletePattern = "*" + OpenTelemetryProfilerProvider.TraceFileExtension, // => "*.nettrace"
                GracePeriod = configuration.TraceScavenger.GracePeriod,
            });
    }
}