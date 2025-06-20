//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal static class ISupportSamplingExtensions
    {
        private readonly static double? NoSampling = 100;

        /// <summary>
        /// Prevent sampling of the given telemetry item by setting its sampling
        /// percentage to 100%
        /// </summary>
        /// <param name="supportSampling">The telemetry item.</param>
        public static void PreventSampling(this ISupportSampling supportSampling) =>
            supportSampling.SamplingPercentage = NoSampling;
    }
}
