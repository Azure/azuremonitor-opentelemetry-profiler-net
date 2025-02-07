using System;
using System.IO;
using Microsoft.Extensions.Options;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class UserCacheManager : IUserCacheManager
{
    private const string ProductName = "ServiceProfiler";
    private const string Uploader = "Uploader";
    private const string ServiceProfilerTempTraceFolderName = "SPTraces";

    private readonly UserConfigurationBase _userConfiguration;
    private readonly IProfilerCoreAssemblyInfo _profilerAssemblyInfo;

    public UserCacheManager(
        IOptions<UserConfigurationBase> userConfiguration,
        IProfilerCoreAssemblyInfo profilerAssemblyInfo)
    {
        _profilerAssemblyInfo = profilerAssemblyInfo ?? throw new ArgumentNullException(nameof(profilerAssemblyInfo));
        _userConfiguration = userConfiguration.Value ?? throw new ArgumentNullException(nameof(userConfiguration));

        string localCacheFolder = GetUserCacheFolder();

        UserCacheDirectory = new DirectoryInfo(localCacheFolder);
        UploaderDirectory = new DirectoryInfo(Path.Combine(_userConfiguration.LocalCacheFolder, ProductName, _profilerAssemblyInfo.Version.ToString(), Uploader));
        TempTraceDirectory = new DirectoryInfo(Path.Combine(localCacheFolder, ServiceProfilerTempTraceFolderName));
    }

    public DirectoryInfo UserCacheDirectory { get; }

    public DirectoryInfo UploaderDirectory { get; }

    public DirectoryInfo TempTraceDirectory { get; }

    private string GetUserCacheFolder()
    {
        string localCacheFolder = _userConfiguration.LocalCacheFolder;
        if (string.IsNullOrEmpty(localCacheFolder))
        {
            localCacheFolder = Path.GetTempPath();
        }
        return localCacheFolder;
    }
}
