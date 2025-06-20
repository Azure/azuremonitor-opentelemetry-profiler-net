//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(ServiceProfilerInjection))]

namespace Microsoft.AspNetCore.Hosting
{
    public class ServiceProfilerInjection : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            if (builder is null)
            {
                throw new System.ArgumentNullException(nameof(builder));
            }

            builder.ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddApplicationInsightsTelemetry();
                serviceCollection.AddServiceProfiler();
            });
        }
    }
}
