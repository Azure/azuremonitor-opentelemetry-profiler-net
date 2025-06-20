//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal static class TelemetryConstants
    {
#if DESKTOPBUILD
        // Test App Insight account.
        // Correspond to App Insight Account: sp-dev-telemetry-profiler-agent
        public static Guid ServiceProfilerAgentIKey = new Guid("55fc42f8-b1cc-4343-9e1d-363994950358");

#else
        // Production App Insight account.
        // Correspond to App Insight Account: sp-telemetry-profiler-agent
        public static Guid ServiceProfilerAgentIKey = new Guid("ad4481a6-e86a-4f34-bf37-35484e63016c");
#endif

        public const string SessionStarted = "Service Profiler session started.";
        public const string SessionEnded = "Service Profiler session finished.";
        public const string TraceUploaded = "Service Profiler trace uploaded.";
        public const string CallTraceUploaderFinished = "Finished calling trace uploader.";
    }
}
