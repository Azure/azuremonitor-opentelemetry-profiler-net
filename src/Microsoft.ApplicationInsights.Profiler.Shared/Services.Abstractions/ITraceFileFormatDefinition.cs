using Microsoft.ServiceProfiler.Contract.Agent.Profiler;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface ITraceFileFormatDefinition
{
    /// <summary>
    /// Gets the file extension for the trace file format. ".nettrace" for example.
    /// </summary>
    public string FileExtension { get; }

    /// <summary>
    /// Gets the name of the trace file. For example, "NetTrace".
    /// Find supported format from <see cref="TraceFileFormat"/>.
    /// </summary>
    public string TraceFileFormatName { get; }
}
