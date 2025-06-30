//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.AspNetCore;
using Microsoft.ApplicationInsights.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ServiceCollectionBuilder : IServiceCollectionBuilder
    {
        public IServiceCollection Build(IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            // In AppInsights code, there is a check to ensure not inject the service twice:
            // Reference: https://github.com/Microsoft/ApplicationInsights-aspnetcore/blob/3dcab5b92ebddc92e9010fc707cc7062d03f92e4/src/Microsoft.ApplicationInsights.AspNetCore/Extensions/ApplicationInsightsExtensions.cs
            services.AddApplicationInsightsTelemetry();

            // Other core services.
            services.AddProfilerCoreServices();

            // Add Service Profiler Background Service
            if (!services.Any(descriptor =>
                descriptor.ImplementationType == typeof(ServiceProfilerBackgroundService)))
            {
                services.AddSingleton<IServiceProfilerAgentBootstrap>(p =>
                {
                    UserConfiguration userConfiguration = p.GetRequiredService<IOptions<UserConfiguration>>().Value;
                    // Choose one by configurations to register.
                    return userConfiguration.IsDisabled ?
                        ActivatorUtilities.CreateInstance<DisabledAgentBootstrap>(p) :
                        ActivatorUtilities.CreateInstance<ServiceProfilerAgentBootstrap>(p);
                });
                services.AddHostedService<ServiceProfilerBackgroundService>();
            }

            return services;
        }
    }
}
