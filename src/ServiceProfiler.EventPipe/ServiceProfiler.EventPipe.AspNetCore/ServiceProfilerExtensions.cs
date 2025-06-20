//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.ApplicationInsights.Profiler.AspNetCore;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceProfilerExtensions
    {
        /// <summary>
        /// Enables the Service Profiler using default configurations.
        /// </summary>
        /// <param name="serviceCollection">Service collection to inject service profiler services in.</param>
        /// <returns>Returns the service collection.</returns>
        public static IServiceCollection AddServiceProfiler(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddServiceProfiler(options: null);
        }

        /// <summary>
        /// Enables the Service Profiler using options pattern.
        /// </summary>
        /// <param name="serviceCollection">Service collection to inject service profiler services in.</param>
        /// <param name="options">Sets the options that contains the custom settings.</param>
        /// <returns>Returns the service collection.</returns>
        public static IServiceCollection AddServiceProfiler(this IServiceCollection serviceCollection, Action<UserConfiguration>? options)
        {
            return AddServiceProfilerImp(serviceCollection, options, configuration: null, serviceCollectionBuilder: null);
        }

        /// <summary>
        /// Enables the Service Profiler using default configurations.
        /// </summary>
        /// <param name="serviceCollection">Service collection to inject service profiler services in.</param>
        /// <param name="configuration">Optional. The configuration instance for the app.</param>
        /// <returns>Returns the service collection.</returns>
        public static IServiceCollection AddServiceProfiler(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            return AddServiceProfilerImp(serviceCollection,
                context: null,
                configuration: configuration,
                serviceCollectionBuilder: null);
        }

        #region private
        // TODO: Consider retire this overload because it is not providing production value.
        /// <summary>
        /// Enables the Service Profiler, using customServiceCollectionBuilder to inject service profiler related services.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="customServiceCollectionBuilder"></param>
        /// <returns></returns>
        internal static IServiceCollection AddServiceProfiler(this IServiceCollection serviceCollection, IServiceCollectionBuilder serviceCollectionBuilder)
        {
            return AddServiceProfilerImp(serviceCollection, context: null, configuration: null, serviceCollectionBuilder: serviceCollectionBuilder);
        }

        private static IServiceCollection AddServiceProfilerImp(
            IServiceCollection serviceCollection,
            Action<UserConfiguration>? context,
            IConfiguration? configuration = null,
            IServiceCollectionBuilder? serviceCollectionBuilder = null)
        {
            try
            {
                // If the service already exists, return immediately.
                if (serviceCollection.Any(descriptor => descriptor.ServiceType == typeof(ServiceProfilerServices)))
                {
                    return serviceCollection;
                }

                // Otherwise, start register necessary services and start Service Profiler
                BuildServiceBase(serviceCollection, context, configuration);
                serviceCollection.AddSingleton<ServiceProfilerServices>();
                serviceCollectionBuilder = serviceCollectionBuilder ?? new ServiceCollectionBuilder();
                serviceCollectionBuilder.Build(serviceCollection);
            }
            // Best effort to register Profiler
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                Console.WriteLine(ex.ToString());
            }

            return serviceCollection;
        }

        private static void BuildServiceBase(
            IServiceCollection serviceCollection,
            Action<UserConfiguration>? applyOptions,
            IConfiguration? configuration = null)
        {
            serviceCollection.AddLogging();
            serviceCollection.AddOptions();

            // Register IOptions<UserConfiguration> and IOptions<UserConfigurationBase>.
            serviceCollection.AddSingleton<IConfigureOptions<UserConfiguration>>(p =>
            {
                return new ConfigureUserConfiguration(configuration ?? p.GetService<IConfiguration>(),
                    applyOptions,
                    p.GetRequiredService<ISerializationProvider>(),
                    p.GetRequiredService<ILogger<ConfigureUserConfiguration>>());
            });

            serviceCollection.AddSingleton(p =>
            {
                UserConfiguration userConfiguration = p.GetRequiredService<IOptions<UserConfiguration>>().Value;
                return Options.Options.Create<UserConfigurationBase>(userConfiguration);
            });
        }

        #endregion
    }
}
