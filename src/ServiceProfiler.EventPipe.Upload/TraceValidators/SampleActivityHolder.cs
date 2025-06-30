//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal class SampleActivityHolder
    {
        public SampleActivityHolder(SampleActivity activity)
        {
            this.SampleActivity = activity;

        }
        public SampleActivity SampleActivity { get; }
        public bool StartActivityHit { get; set; }
        public bool StopActivityHit { get; set; }
    }
}