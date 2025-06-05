using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Core.IPC;
using Microsoft.ServiceProfiler.DataContract.Settings;

namespace Microsoft.ApplicationInsights.Profiler.Core.Contracts
{
    /// <summary>
    /// This is the data contract to accept user configurations from IConfigure. Ideally, every settings should be optional,
    /// and default values should have been provided.
    /// For example:
    /// {
    ///     "ServiceProfiler": {
    ///         "IsDisabled": false,
    ///         "BufferSizeInMB": "250",
    ///         "Duration": "00:00:30",
    ///         "Interval": "00:29:30",
    ///         "InitialDelay": "00:00:10",
    ///         "ProvideAnonymousTelemetry": false,
    ///         "IsSkipCompatibilityTest": false,
    ///         "Endpoint": "https://agent.azureserviceprofiler.net",
    ///         "SkipEndpointCertificateValidation": false,
    ///         "PreserveTraceFile": false,
    ///         "LocalCacheFolder": "/tmp/",
    ///         "UploadMode": "OnSuccess",
    ///         "UploaderEnvironment": "Production",
    ///         "NamedPipe": {
    ///             "ConnectionTimeout": "00:00:30"
    ///         },
    ///         "TraceScavenger": {
    ///             "InitialDelay": "00:05:00"
    ///             "Interval": "00:15:00"
    ///         }
    ///     }
    /// }
    /// </summary>
    public class UserConfiguration
    {
        /// <summary>
        /// Gets or sets the circular buffer for traces in memory.
        /// For 2 minutes profiling, the average size of the trace is less than 100MB.
        /// Optional, default value is 250.
        /// </summary>
        public int BufferSizeInMB { get; set; } = 250;

        /// <summary>
        /// Gets or sets the duration of one profiling session for all policies.
        /// Optional, default value is 2 minutes.
        /// </summary>
        /// <returns></returns>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Gets or sets the initial delay before starting the first profiling session for all policies.
        /// Optional, default value is 0.
        /// </summary>
        public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets or sets the frequency for configuration updates for triggers or on demand profiling.
        /// This configuration decides how frequent the agent pulls configurations from the server.
        /// Optional, default value is 5 seconds.
        /// </summary>
        public TimeSpan ConfigurationUpdateFrequency { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether to send anonymous telemetry data to Microsoft to make the product better.
        /// Optional, default value is false.
        /// </summary>
        public bool ProvideAnonymousTelemetry { get; set; } = false;

        /// <summary>
        /// Gets or sets the value to enable or disable the Service Profiler. Service Profiler will be disabled when the set to true.
        /// Optional, default value is false.
        /// </summary>
        public bool IsDisabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the overhead for random profiling.
        /// The rate is used to calculate the time of profiling in average per hour.
        /// Basically, n = (60 * overhead rate) / profiling-duration
        /// </summary>
        public float RandomProfilingOverhead { get; set; } = 0.05F;

        /// <summary>
        /// Gets or sets the value to enable or disable the compatibility test before invoking the Service Profiler. Service Profiler will be disabled
        /// when it fail the compatibility test unless this value is set to true.
        /// Optional, default value is false.
        /// </summary>
        /// <remarks>The current compatibility testing depends on the framework description provided by the RuntimeInformation and it could be changed.
        /// Refer https://github.com/dotnet/corefx/issues/9725 for details. This property could be used as an escape when the change happens.
        /// </remarks>
        public bool IsSkipCompatibilityTest { get; set; } = false;

        /// <summary>
        /// Gets or sets the Service Profiler endpoint.
        /// Optional, default value is pointing to the well-known production profiler server endpoint.
        /// </summary>
        public string Endpoint { get; set; } = null;

        /// <summary>
        /// Deprecated. Please use UploadMode settings instead. This option will be removed in later versions.
        /// Gets or sets the flag to skip uploading the trace when a profiling session is done.
        /// Optional, default value is false.
        /// </summary>
        [Obsolete("Deprecated. Use UploadMode instead. This option will be removed in the future version.", error: false)]
        public bool SkipUpload
        {
            get
            {
                return UploadMode == UploadMode.Never;
            }
            set
            {
                if (value && UploadMode == UploadMode.OnSuccess)
                {
                    UploadMode = UploadMode.Never;
                }
            }
        }

        /// <summary>
        /// Gets or sets the upload mode. Valid values are: Never, OnSuccess and Always.
        /// Optional, default value is UploadOnSuccess.
        /// </summary>
        /// <remarks>
        /// It is recommended to keep this value to default in a production environment. The intention for this option is purely based on
        /// debugging scenarios.
        /// </remarks>
        public UploadMode UploadMode { get; set; } = UploadMode.OnSuccess;

        /// <summary>
        /// Gets or sets the value to preserve the trace file after it being uploaded.
        /// Optional, default value is false.
        /// </summary>
        public bool PreserveTraceFile { get; set; } = false;

        /// <summary>
        /// Gets or sets the value to skip the certificate validation to establish SSL communication
        /// with the Endpoint.
        /// Optional, default value is false.
        /// </summary>
        public bool SkipEndpointCertificateValidation { get; set; } = false;

        /// <summary>
        /// Percentage threshold for the CPU trigger scheduling policy. Profiling is activated if avg. CPU usage over a period of time exceeds this.
        /// Optional, default value is 80 (%)
        /// </summary>
        public float CPUTriggerThreshold { get; set; } = 80.0F;

        /// <summary>
        /// How long to wait until profiling again
        /// </summary>
        public TimeSpan CPUTriggerCooldown { get; set; } = TimeSpan.FromSeconds(CpuTriggerSettings.DEFAULT_CPU_TRIGGER_COOLDOWN_IN_SECONDS);

        /// <summary>
        /// Percentage threshold for the Memory trigger scheduling policy. Profiling is activated if avg. RAN usage over a period of time exceeds this.!--
        /// </summary>
        public float MemoryTriggerThreshold { get; set; } = 80.0F;

        /// <summary>
        /// How long to wait until profiling again
        /// </summary>
        public TimeSpan MemoryTriggerCooldown { get; set; } = TimeSpan.FromSeconds(MemoryTriggerSettings.DEFAULT_MEMORY_TRIGGER_COOLDOWN_IN_SECONDS);

        /// <summary>
        /// Allows the agent to run without talking to Profiler service (The frontend).
        /// </summary>
        public bool StandaloneMode { get; set; } = false;

        /// <summary>
        /// Gets or sets the working folder to store trace files temporary.
        /// This will allow to specify a different folder than default one.
        /// </summary>
        public string LocalCacheFolder { get; set; } = Path.GetTempPath();

        /// <summary>
        /// Gets or sets whether unhandled exception leads to crash.
        /// Default value is false, that unhandled exception will be swallowed to avoid crashing of the application.
        /// This is not best practice. However, we don't want unhandled exception in Profiler to disrupt the user's service.
        /// The intend to provide this switch: Allows the user choose to set this to true and to get a crash dump for debugging Profiler.
        /// </summary>
        public bool AllowsCrash { get; set; } = false;

        /// <summary>
        /// Get or sets the environment for the uploader.
        /// </summary>
        public string UploaderEnvironment { get; set; } = "Production";

        /// <summary>
        /// NamedPipe options.
        /// </summary>
        public NamedPipeOptions NamedPipe { get; set; } = new NamedPipeOptions();

        /// <summary>
        /// Gets or sets the Custom EventPipeProviders. These are EventPipe providers in addition to the built-in event pipe providers.
        /// Go to https://aka.ms/ep-sp/custom-providers for more details.
        /// </summary>
        public IEnumerable<EventPipeProviderItem> CustomEventPipeProviders { get; set; } = null;

        /// <summary>
        /// Gets or sets the trace scavenger service options.
        /// </summary>
        public TraceScavengerServiceOptions TraceScavenger { get; set; } = new TraceScavengerServiceOptions();
    }
}
