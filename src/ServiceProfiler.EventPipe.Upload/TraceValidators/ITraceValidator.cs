using System.Collections.Generic;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal interface ITraceValidator
    {
        /// <summary>
        /// Validate the trace. Throws exception when validation failed.
        /// </summary>
        /// <returns>The samples that matches the trace.</returns>
        IEnumerable<SampleActivity> Validate(IEnumerable<SampleActivity> samples);

        /// <summary>
        /// Extension point to plug in more validators
        /// </summary>
        ITraceValidator? Next { get; }
    }
}
