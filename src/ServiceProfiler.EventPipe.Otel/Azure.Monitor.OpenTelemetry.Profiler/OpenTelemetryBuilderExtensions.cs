// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Monitor.OpenTelemetry.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Azure.Monitor.OpenTelemetry.Profiler;

public static class OpenTelemetryBuilderExtensions
{
    /// <summary>
    /// Register the services needed to enable Profiler. Use this when IOpenTelemetryBuilder
    /// is not available.
    /// </summary>
    /// <param name="builder">A trace provider builder.</param>
    /// <param name="configureServiceProfiler">An action to customize the behavior of the profiler.</param>
    public static TracerProviderBuilder UseProfiler(this TracerProviderBuilder builder, Action<ServiceProfilerOptions>? configureServiceProfiler = null) 
        => builder.ConfigureServices(services => ConfigureServices(services, configureServiceProfiler));

    /// <summary>
    /// Register the services needed to enable Profiler.
    /// </summary>
    /// <param name="builder">The service container.</param>
    /// <param name="configureServiceProfiler">An action to customize the behavior of the profiler.</param>
    public static IOpenTelemetryBuilder UseProfiler(this IOpenTelemetryBuilder builder, Action<ServiceProfilerOptions>? configureServiceProfiler = null)
    {
        ConfigureServices(builder.Services, configureServiceProfiler);
        return builder;
    }

    private static void ConfigureServices(IServiceCollection services, Action<ServiceProfilerOptions>? configureServiceProfiler)
    {
        services.AddLogging();
        services.AddOptions();

        services.AddOptions<ServiceProfilerOptions>().Configure<IConfiguration, IOptions<AzureMonitorExporterOptions>>((opt, configuration, azureMonitorOptions) =>
        {
            configuration.GetSection("ServiceProfiler").Bind(opt);

            AzureMonitorExporterOptions? monitorOptions = azureMonitorOptions.Value;

            // Inherit connection string from the Azure Monitor Options unless
            // the value is already there.
            string? azureMonitorConnectionString = monitorOptions.ConnectionString;
            if (string.IsNullOrWhiteSpace(opt.ConnectionString))
            {
                opt.ConnectionString = azureMonitorConnectionString;
            }

            // Inherit the credential object from the Azure Monitor Options unless
            // the value is already there.
            opt.Credential ??= monitorOptions.Credential;
            configureServiceProfiler?.Invoke(opt);

            // Last effort to capture the connection string when all above failed
            if (string.IsNullOrEmpty(opt.ConnectionString))
            {
                opt.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
            }

            // Fast fail when the connection string is not set.
            // This should never happen, or the profiler is not going to work.
            if (string.IsNullOrEmpty(opt.ConnectionString))
            {
                throw new InvalidOperationException("Connection string can't be fetched. Please follow the instructions to setup connection string properly.");
            }
        });

        services.AddSingleton<IOptions<UserConfigurationBase>>(p =>
        {
            ServiceProfilerOptions profilerOptions = GetRequiredOptions<ServiceProfilerOptions>(p);
            return Options.Create(profilerOptions);
        });

        services.AddServiceProfilerCore();

        services.AddSingleton<IServiceProfilerAgentBootstrap>(p =>
        {
            ServiceProfilerOptions userConfiguration = GetRequiredOptions<ServiceProfilerOptions>(p);
            // Choose one by configurations to register.
            return userConfiguration.IsDisabled ?
                ActivatorUtilities.CreateInstance<DisabledAgentBootstrap>(p) :
                ActivatorUtilities.CreateInstance<ServiceProfilerAgentBootstrap>(p);
        });

        services.AddHostedService<ProfilerBackgroundService>();
    }

    private static T GetRequiredOptions<T>(IServiceProvider p)
        where T : class
    {
        return p.GetRequiredService<IOptions<T>>().Value ?? throw new InvalidOperationException($"Option {typeof(T).FullName} is required.");
    }
}


