using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(Microsoft.ApplicationInsights.Profiler.AspNetCore.HostingStartup30.ProfilerBootstrapper))]

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore.HostingStartup30
{
    public class ProfilerBootstrapper : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.ConfigureServices(services =>
            {
                services.AddApplicationInsightsTelemetry();
                services.AddServiceProfiler();
            });
        }
    }
}
