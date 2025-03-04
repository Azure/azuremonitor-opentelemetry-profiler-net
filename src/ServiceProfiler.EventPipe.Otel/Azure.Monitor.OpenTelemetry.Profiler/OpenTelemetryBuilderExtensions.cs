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

namespace Azure.Monitor.OpenTelemetry.Profiler;

public static class OpenTelemetryBuilderExtensions
{
    /// <summary>
    /// Register the services needed to enable Profiler.
    /// </summary>
    /// <param name="builder">The service container.</param>
    /// <param name="configureServiceProfiler">An action to customize the behavior of the profiler.</param>
    public static IOpenTelemetryBuilder AddAzureMonitorProfiler(this IOpenTelemetryBuilder builder, Action<ServiceProfilerOptions>? configureServiceProfiler = null)
    {
        if (!PreCheck(builder.Services))
        {
            // Did not pass the registering check but okay to let the application keep running.
            return builder;
        }

        // Passed the registering check, keep registering classes into the 
        ConfigureServices(builder.Services, configureServiceProfiler);
        return builder;
    }

    /// <summary>
    /// Verify if the register needs to be run.
    /// There are 2 responsibilities:
    /// 1. to avoid double registering when called in multiple places.
    /// 2. to verify that dependency services are there.
    /// </summary>
    /// <param name="services"></param>
    /// <returns>Returns true when register should continue. Otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">The application should be interrupted for invalid configurations. Check out the reasons by the message.</exception>
    private static bool PreCheck(IServiceCollection services)
    {
        bool isRegistered = IsRegistered(services);
        if(isRegistered)
        {
            // Fail the pre-check.
            return false;
        }

        // TODO: Looking into better ways to check dependencies
        // It is not very clear what to do today.
        return true;
    }

    // The services are treated as registered before when the bootstrap service is registered.
    // In the future, when we need more complex check, consider injecting the logic than simply update this
    // implementation.
    private static bool IsRegistered(IServiceCollection services) =>
        services.Any(descriptor => descriptor.ServiceType == typeof(IServiceProfilerAgentBootstrap));

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


