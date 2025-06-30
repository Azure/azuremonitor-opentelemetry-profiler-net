using System;
using System.IO;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal class UploaderLocatorByUnzipping : UploadLocatorBase
{
    private const string TraceUploaderArchiveFileName = "TraceUpload.zip";
    private readonly IUserCacheManager _userCacheManager;
    private readonly IProfilerCoreAssemblyInfo _profilerCoreAssemblyInfo;
    private readonly IZipFile _zipFileService;

    public UploaderLocatorByUnzipping(
        IUserCacheManager userCacheManager,
        IProfilerCoreAssemblyInfo profilerCoreAssemblyInfo,
        IFile fileService,
        IZipFile zipFileService,
        ILogger<UploaderLocatorByUnzipping> logger)
        : base(fileService, logger)
    {
        _userCacheManager = userCacheManager ?? throw new ArgumentNullException(nameof(userCacheManager));
        _profilerCoreAssemblyInfo = profilerCoreAssemblyInfo ?? throw new ArgumentNullException(nameof(profilerCoreAssemblyInfo));
        _zipFileService = zipFileService ?? throw new ArgumentNullException(nameof(zipFileService));
    }

    public override int Priority => 40;

    protected override string? GetUploaderFullPath()
    {
        Logger.LogTrace("ProfilerCoreAssemblyInfo.Directory.FullName: {value}", _profilerCoreAssemblyInfo.Directory.FullName);

        string? uploaderPath = null;
        foreach (string zipFolder in GetZipDirectories())
        {
            uploaderPath = GetUploaderFullPath(zipFolder);
            if (!string.IsNullOrEmpty(uploaderPath))
            {
                // Extracted uploader is found. Stop searching.
                break;
            }
        }

        return uploaderPath;
    }

    private string? GetUploaderFullPath(string zipFolder)
    {
        string zipPath = Path.Combine(zipFolder, TraceUploaderArchiveFileName);

        if (FileService.Exists(zipPath))
        {
            Logger.LogDebug("Found zipped uploader at {filePath}. Extracting...", zipPath);
            string targetFolder = _userCacheManager.UploaderDirectory.FullName;
            Directory.CreateDirectory(targetFolder);

            try
            {
                _zipFileService.ExtractToDirectory(zipPath, targetFolder);
            }
            catch (IOException ex)
            {
                Logger.LogWarning(ex, "Extracting uploader failed. Is the target directory {folder} exists or is it writable?", targetFolder);
            }

            string unzippedUploaderPath = Path.Combine(targetFolder, TraceUploaderAssemblyName);
            if (FileService.Exists(unzippedUploaderPath))
            {
                return unzippedUploaderPath;
            }
            else
            {
                Logger.LogDebug("Target unzipped uploader not found at {filePath}", unzippedUploaderPath);
                return null;
            }
        }
        else
        {
            Logger.LogDebug("Zipped uploader's not found at {filePath}", zipPath);
            return null;
        }
    }

    /// <summary>
    /// Returns a list of possible zipped uploader folders.
    /// </summary>
    internal IEnumerable<string> GetZipDirectories()
    {
        // Consider:
        // Providing a list of possible zipped uploader folders is a responsibility different than the main responsibility of this class to locate the extracted uploader.
        // Pull this part out to its own ZippedUploaderPathProvider if changes happens a lot to this method for better OC implementation.
        string currentBinPath = _profilerCoreAssemblyInfo.Directory.FullName;
        yield return Path.Combine(currentBinPath, "ServiceProfiler");
        yield return currentBinPath;
    }
}
