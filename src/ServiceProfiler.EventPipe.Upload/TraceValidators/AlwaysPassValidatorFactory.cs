using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.Stubs
{
    internal class AlwaysPassValidatorFactory : ITraceValidatorFactory
    {
        public ITraceValidator Create(string traceFilePath)
        {
            return new AlwaysPassValidator();
        }
    }
}
