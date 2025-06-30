//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Agent;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.Common.Utilities;
using ServiceProfiler.EventPipe.Upload;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<UploadContext>(args).WithParsed<UploadContext>(options =>
            {
                using IHost host = CreateHostBuilder(args, options).Build();
                host.Run();
            });
        }

        private static IHostBuilder CreateHostBuilder(string[] args, UploadContext options) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config =>
                {
                    config.AddCommandLine(args);
                })
                .ConfigureLogging((context, logging) =>
                {
                    // Clear all previously registered providers.
                    logging.ClearProviders();

                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        Console.WriteLine("Uploader Hosting Environment is set to: Development.");
                        logging.AddSimpleConsole(configure =>
                        {
                            configure.SingleLine = true;
                        }).SetMinimumLevel(LogLevel.Trace);
                        logging.AddDebug().SetMinimumLevel(LogLevel.Trace);
                    }
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddSingleton(options); // TODO: Look into migrate to IOptions pattern.
                    ConfigureServiceCollection(services, options);

                    // Adding hosted service here:
                    services.AddHostedService<HostedUploaderService>();
                });

        private static void ConfigureServiceCollection(IServiceCollection services, UploadContext options)
        {
            services.AddOptions<IngestionClientOptions>().Configure(config =>
            {
                config.Endpoint = options.HostUrl;
                config.UserAgent = "EventPipeProfilerUploader";
                config.AllowInsecureSslConnection = options.SkipEndpointCertificateValidation;
                config.ApiVersion = "2023-01-10";
            });

            // Utilities
            services.TryAddSingleton<IAppProfileClientFactory, AppProfileClientFactory>();

            services.TryAddTransient<IUploadContextValidator, UploadContextValidator>();
            services.TryAddSingleton<IZipUtility, ZipUtility>();
            services.AddSingleton<IOSPlatformProvider, OSPlatformProvider>();

            services.TryAddTransient<ISerializationProvider, HighPerfJsonSerializationProvider>();
            services.TryAddTransient<ISampleActivitySerializer, SampleActivitySerializer>();
            services.TryAddTransient<IPayloadSerializer, HighPerfJsonSerializationProvider>();
            services.TryAddSingleton<INamedPipeServerFactory, NamedPipeServerFactory>();

            // Telemetry
            // Todo: Allow user to opt out telemetry
            services.TryAddSingleton<IAppInsightsLogger>(provider =>
            {
                IAppInsightsLogger logger = new EventPipeAppInsightsLogger(
                    TelemetryConstants.ServiceProfilerAgentIKey);
                UploadContext uploaderContext = provider.GetRequiredService<UploadContext>();

                logger.SetCommonProperty(Constants.SessionId, uploaderContext.SessionId.ToString("o", CultureInfo.InvariantCulture));
                logger.SetCommonProperty(Constants.OS, RuntimeInformation.OSDescription);
                logger.SetCommonProperty(Constants.Runtime, RuntimeInformation.FrameworkDescription);
                // TODO: To restore the functionality, it requires to query the data cube async here.
                // Having an async operation is not a good idea.
                // Make the IngestionClient a dependency of EventPipeAppInsightsLogger makes more sense.
                // logger.SetCommonProperty(Constants.AuthenticatedUserId, uploaderContext.DataCube.ToString());
                string componentVersion = EnvironmentUtilities.ExecutingAssemblyInformationalVersion;
                logger.SetCommonProperty(Constants.ComponentVersion, componentVersion);

                if (CurrentProcessUtilities.TryGetId(out int? pid))
                {
                    logger.SetCommonProperty(Constants.ProcessId, pid!.Value.ToString(CultureInfo.InvariantCulture));
                }

                logger.SetCommonProperty(Constants.CloudRoleInstance, EnvironmentUtilities.HashedMachineNameWithoutSiteName);

                return logger;
            });

            services.TryAddTransient<IProfilerFrontendClientBuilder, ProfilerFrontendClientBuilder>();
            services.TryAddSingleton<IBlobClientFactory, BlobClientFactory>();
            services.TryAddSingleton<ITraceValidatorFactory, EventPipeTraceValidatorFactory>();

            services.AddSingleton<TraceUploaderFactory>();

            services.AddTransient<ICustomEventsSender, CustomEventsSender>();
        }
    }
}
