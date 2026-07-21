// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.Common.Utilities;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal class EventPipeAppInsightsLogger : IAppInsightsLogger, IDisposable
    {
        private ConnectionString? _connectionString;
        private readonly TelemetryClient _telemetryClient;
        private readonly TelemetryConfiguration _telemetryConfiguration;
        private bool _isDisposed = false;

        /// <summary>
        /// Creates a logger that sends telemetry using the given connection string.
        /// This is intended for Microsoft's anonymous agent-usage telemetry (see
        /// TelemetryConstants.ServiceProfilerAgentConnectionString), not customer telemetry.
        /// </summary>
        public EventPipeAppInsightsLogger(string connectionString)
            : this(CreateConnectionStringConfiguration(connectionString))
        {
        }

        /// <summary>
        /// Creates a logger that sends telemetry using a fully configured <see cref="TelemetryConfiguration"/>.
        /// Use this overload for customer telemetry so the regional IngestionEndpoint and AAD token credential
        /// (when configured) are honored. Build the configuration with TelemetryConfigurationBuilder.
        /// </summary>
        public EventPipeAppInsightsLogger(TelemetryConfiguration telemetryConfiguration)
        {
            _telemetryConfiguration = telemetryConfiguration ?? throw new ArgumentNullException(nameof(telemetryConfiguration));
            _telemetryClient = new TelemetryClient(_telemetryConfiguration);

            // Reference https://github.com/Microsoft/ApplicationInsights-Home/blob/master/EndpointSpecs/SDK-VERSIONS.md for SDK Name.
            string sdkVersion = EnvironmentUtilities.GetApplicationInsightsSdkVersion("l_ap:");
            SetCommonProperty(Constants.SdkVersion, sdkVersion);
        }

        private static TelemetryConfiguration CreateConnectionStringConfiguration(string connectionString)
            => new TelemetryConfiguration
            {
                ConnectionString = connectionString,
            };

        /// <summary>
        /// Get and set the instrumentation key. By setting it to 'null' you can disable the logger.
        /// </summary>
        public ConnectionString? ConnectionString
        {
            get
            {
                return _connectionString ?? (ConnectionString.TryParse(_telemetryConfiguration.ConnectionString, out _connectionString) ? _connectionString : default(ConnectionString));
            }
            set
            {
                if (_connectionString != value)
                {
                    _connectionString = value;
                    _telemetryConfiguration.ConnectionString = value?.ToString();
                }
            }
        }

        public Type TelemetryChannelType => _telemetryConfiguration.TelemetryChannel.GetType();

        public void SetCommonProperty(string key, string value)
        {
            var context = _telemetryClient.Context;

            switch (key)
            {
                case Constants.SessionId:
                    context.Session.Id = value;
                    break;

                case Constants.ComponentVersion:
                    context.Component.Version = value;
                    break;

                case Constants.CloudRoleInstance:
                    context.Cloud.RoleInstance = value;
                    break;

                case Constants.OS:
                    context.Device.OperatingSystem = value;
                    break;

                case Constants.AuthenticatedUserId:
                    context.User.AuthenticatedUserId = value;
                    break;

                case Constants.SdkVersion:
                    context.GetInternalContext().SdkVersion = value;
                    break;

                default:
                    context.GlobalProperties[key] = value;
                    break;
            }
        }

        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null, bool preventSampling = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            var eventTelemetry = new EventTelemetry(eventName);
            MergeDictionaries(eventTelemetry.Properties, properties);
            MergeDictionaries(eventTelemetry.Metrics, metrics);

            if (preventSampling)
            {
                eventTelemetry.PreventSampling();
            }

            _telemetryClient.TrackEvent(eventTelemetry);
        }

        public void TrackException(Exception exception, string operationName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            if (!IsEnabled)
            {
                return;
            }

            var exceptionTelemetry = new ExceptionTelemetry(exception);

            if (operationName != null)
            {
                exceptionTelemetry.Context.Operation.Name = operationName;
            }

            MergeDictionaries(exceptionTelemetry.Properties, properties);
            MergeDictionaries(exceptionTelemetry.Metrics, metrics);

            _telemetryClient.TrackException(exceptionTelemetry);
        }

        public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string>? properties = null, bool preventSampling = false)
        {
            if (!IsEnabled)
            {
                return;
            }

            var traceTelemetry = new TraceTelemetry(message, severityLevel);
            MergeDictionaries(traceTelemetry.Properties, properties);

            if (preventSampling)
            {
                traceTelemetry.PreventSampling();
            }

            _telemetryClient.TrackTrace(traceTelemetry);
        }

        public void Flush()
        {
            // Give it 1 second just to allow the telemetry being flushed.
            // Workaround for issue: https://github.com/Microsoft/ApplicationInsights-dotnet/issues/407.
            // There is still no guarantee that all events will be sent. However, given limited time, this is a reasonable workaround.
            Flush(TimeSpan.FromSeconds(1));
        }

        private bool IsEnabled => ConnectionString != default(ConnectionString);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_isDisposed) { return; }

            if (isDisposing)
            {
                Flush();
                _telemetryConfiguration.Dispose();
            }

            _isDisposed = true;
        }

        private static void MergeDictionaries<K, V>(IDictionary<K, V> target, IDictionary<K, V>? dictionaryToMerge)
        {
            if (dictionaryToMerge != null)
            {
                foreach (var kvp in dictionaryToMerge)
                {
                    if (target.ContainsKey(kvp.Key))
                    {
                        target[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        target.Add(kvp);
                    }
                }
            }
        }

        private void Flush(TimeSpan delayPostFlush)
        {
            _telemetryClient?.Flush();
            Thread.Sleep(delayPostFlush);
        }
    }
}
