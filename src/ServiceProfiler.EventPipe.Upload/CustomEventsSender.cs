using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Microsoft.Extensions.Logging;
using ServiceProfiler.Common.Utilities;
using System;
using System.Globalization;
using System.Linq;

namespace ServiceProfiler.EventPipe.Upload;

internal class CustomEventsSender : ICustomEventsSender
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public CustomEventsSender(
        ILoggerFactory loggerFactory,
        ILogger<CustomEventsSender> logger
        )
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Send(UploadContextExtension context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (!ShouldSendCustomEvents(context))
        {
            return;
        }

        _logger.LogInformation("Sending custom events");
        using TelemetryConfiguration telemetryConfiguration = BuildTelemetryConfiguration(context);

        TelemetryClient telemetryClient = new(telemetryConfiguration);

        // Multiple samples
        foreach (ServiceProfilerSample sample in context.AdditionalData?.ServiceProfilerSamples ?? [])
        {
            ServiceProfilerSample sampleUpdate = sample with
            {
                ServiceProfilerContent = sample.ServiceProfilerContent.Replace("%StampId%", context.UploadContext.StampId)
            };

            try
            {
                SendServiceProfilerSamples(sampleUpdate, telemetryClient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send custom event. Sample: {sample}", sampleUpdate);
            }
        }

        if (context.AdditionalData?.ServiceProfilerIndex is null)
        {
            _logger.LogWarning("No ServiceProfilerIndex found in AdditionalData. Skipping index event sending.");
            return;
        }

        // Single index
        ServiceProfilerIndex index = context.AdditionalData.ServiceProfilerIndex with
        {
            StampId = context.UploadContext.StampId,
        };

        try
        {
            SendServiceProfilerIndex(index, telemetryClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send custom event. Index: {index}", index);
        }

        telemetryConfiguration.TelemetryChannel.Flush();
    }

    private TelemetryConfiguration BuildTelemetryConfiguration(UploadContextExtension context)
    {
        string connectionStringValue = context.AdditionalData?.ConnectionString ?? throw new InvalidOperationException("Connection string is required in the upload context.");
        if (!ConnectionString.TryParse(connectionStringValue, out ConnectionString connectionString))
        {
            throw new InvalidCastException($"Connection string {connectionStringValue} is invalid.");
        }

        TelemetryConfigurationBuilder telemetryConfigurationBuilder = new(
            connectionString,
            context.TokenCredential,
            telemetryInitializers: [new PreventSamplingTelemetryInitializer()],
            _loggerFactory.CreateLogger<TelemetryConfigurationBuilder>()
            );

        TelemetryConfiguration telemetryConfiguration = telemetryConfigurationBuilder.Build();

        return telemetryConfiguration;
    }

    private void SendServiceProfilerSamples(ServiceProfilerSample sample, TelemetryClient telemetryClient)
    {
        EventTelemetry eventTelemetry = new("ServiceProfilerSample")
        {
            Timestamp = sample.Timestamp
        };
        eventTelemetry.Properties.Add("ServiceProfilerContent", sample.ServiceProfilerContent);
        eventTelemetry.Properties.Add("ServiceProfilerVersion", "v2");
        eventTelemetry.Properties.Add("RequestId", sample.RequestId);

        if (!string.IsNullOrEmpty(sample.RoleInstance))
        {
            eventTelemetry.Context.Cloud.RoleInstance = sample.RoleInstance;
        }

        if (!string.IsNullOrEmpty(sample.RoleName))
        {
            eventTelemetry.Context.Cloud.RoleName = sample.RoleName;
        }

        if (!string.IsNullOrEmpty(sample.OperationName))
        {
            eventTelemetry.Context.Operation.Name = sample.OperationName;
        }

        if (!string.IsNullOrEmpty(sample.OperationId))
        {
            eventTelemetry.Context.Operation.Id = sample.OperationId;
        }

        telemetryClient.TrackEvent(eventTelemetry);
    }

    private void SendServiceProfilerIndex(ServiceProfilerIndex index, TelemetryClient telemetryClient)
    {
        EventTelemetry eventTelemetry = new("ServiceProfilerIndex")
        {
            Timestamp = index.Timestamp
        };

        eventTelemetry.Properties["FileId"] = index.FileId;
        eventTelemetry.Properties["StampId"] = index.StampId;
        eventTelemetry.Properties["DataCube"] = index.DataCube;
        eventTelemetry.Properties["EtlFileSessionId"] = index.EtlFileSessionId;
        eventTelemetry.Properties["MachineName"] = index.MachineName;
        eventTelemetry.Properties["ProcessId"] = index.ProcessId.ToString(CultureInfo.InvariantCulture);

        // More info here.
        eventTelemetry.Properties["Source"] = index.Source;
        eventTelemetry.Properties["OperatingSystem"] = index.OperatingSystem;
        eventTelemetry.Metrics["AverageCPUUsage"] = index.AverageCPUUsage;
        eventTelemetry.Metrics["AverageMemoryUsage"] = index.AverageMemoryUsage;
        eventTelemetry.Context.Cloud.RoleName ??= index.CloudRoleName;
        eventTelemetry.Context.Cloud.RoleInstance ??= index.MachineName;

        telemetryClient.TrackEvent(eventTelemetry);
    }

    private bool ShouldSendCustomEvents(UploadContextExtension context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }
        switch (context.UploadContext.UploadMode)
        {
            case UploadMode.Always:
                return true;
            case UploadMode.OnSuccess:
                return context.AdditionalData?.ServiceProfilerSamples?.Any() == true &&
                       !string.IsNullOrEmpty(context.AdditionalData?.ConnectionString);
            case UploadMode.Never:
                _logger.LogWarning("Custom events are not sent. Set uploadMode to Always or OnSuccess to send custom events.");
                return false;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized upload mode of: {context.UploadContext.UploadMode}. Please file an issue for investigation.");
        }
    }
}
