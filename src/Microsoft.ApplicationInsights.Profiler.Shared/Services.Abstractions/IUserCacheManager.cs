using System.IO;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IUserCacheManager
{
    DirectoryInfo TempTraceDirectory { get; }
    DirectoryInfo UserCacheDirectory { get; }
    DirectoryInfo UploaderDirectory { get; }
}

