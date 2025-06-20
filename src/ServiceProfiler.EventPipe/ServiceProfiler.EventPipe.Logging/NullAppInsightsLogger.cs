using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;
using ServiceProfiler.Common.Utilities;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal class NullAppInsightsLogger : IAppInsightsLogger
    {
        public ConnectionString? ConnectionString { get; set; }

        public Type? TelemetryChannelType => null;

        public void Flush()
        {
            // Do nothing
        }

        public void SetCommonProperty(string key, string value)
        {
            // Do nothing
        }

        public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null, bool preventSampling = false)
        {
            // Do nothing
        }

        public void TrackException(Exception exception, string operationName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
        {
            // Do nothing
        }

        public void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string>? properties = null, bool preventSampling = false)
        {
            // Do nothing
        }
    }
}
