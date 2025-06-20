using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.Profiler.Core.Utilities;

internal class PreventSamplingTelemetryInitializer : ITelemetryInitializer
{
    private static readonly double? NoSampling = 100.0;

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportSampling supportSampling)
        {
            supportSampling.SamplingPercentage = NoSampling;
        }
    }
}
