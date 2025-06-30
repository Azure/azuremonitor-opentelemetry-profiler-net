using System;
using System.IO;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IProfilerCoreAssemblyInfo
{
    Version Version { get; }
    DirectoryInfo Directory { get; }
}