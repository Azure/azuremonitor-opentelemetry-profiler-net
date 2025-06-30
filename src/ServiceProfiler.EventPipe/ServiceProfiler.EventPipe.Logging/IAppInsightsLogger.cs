//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.DataContracts;
using ServiceProfiler.Common.Utilities;
using System;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal interface IAppInsightsLogger
    {
        /// <summary>
        /// By setting it to 'null' you can disable the logger.
        /// </summary>
        ConnectionString? ConnectionString { get; set; }

        /// <summary>
        /// Set common properties that will be shared by all telemetry events.
        /// </summary>
        void SetCommonProperty(string key, string value);

        void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null, bool preventSampling = false);

        void TrackException(Exception exception, string operationName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);

        void TrackTrace(string message, SeverityLevel severityLevel, IDictionary<string, string>? properties = null, bool preventSampling = false);

        Type? TelemetryChannelType { get; }

        void Flush();
    }
}
