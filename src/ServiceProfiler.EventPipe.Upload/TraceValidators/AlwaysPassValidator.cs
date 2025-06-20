using System.Collections.Generic;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.Stubs
{
    internal class AlwaysPassValidator : ITraceValidator
    {
        public ITraceValidator? Next => null;

        public IEnumerable<SampleActivity> Validate(IEnumerable<SampleActivity> samples)
        {
            // I do nothing;
            return samples;
        }
    }
}
