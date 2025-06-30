using System;
using System.IO;
using System.Reflection;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;
internal sealed class ProfilerCoreAssemblyInfo : IProfilerCoreAssemblyInfo
{
    private ProfilerCoreAssemblyInfo() { }
    public static ProfilerCoreAssemblyInfo Instance { get; } = new ProfilerCoreAssemblyInfo();

    Lazy<Assembly> _thisAssembly = new(() => typeof(ProfilerCoreAssemblyInfo).Assembly);

    public Version Version => _thisAssembly.Value.GetName().Version;

    public DirectoryInfo Directory => new DirectoryInfo(Path.GetDirectoryName(_thisAssembly.Value.Location));
}
