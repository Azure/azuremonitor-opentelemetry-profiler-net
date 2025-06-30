using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class CurrentTraceFileFormat : ITraceFileFormatDefinition
{
    public string FileExtension => ".nettrace";

    public string TraceFileFormatName => TraceFileFormat.Nettrace;
}