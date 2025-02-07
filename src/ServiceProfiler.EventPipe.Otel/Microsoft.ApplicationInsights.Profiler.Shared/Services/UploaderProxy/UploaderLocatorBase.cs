using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal abstract class UploadLocatorBase : IPrioritizedUploaderLocator
{
    protected const string TraceUploaderAssemblyName = "Microsoft.ApplicationInsights.Profiler.Uploader.dll";
    protected IFile FileService { get; }
    protected ILogger Logger { get; }

    public UploadLocatorBase(
        IFile fileService,
        ILogger<UploadLocatorBase> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        FileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    }

    public abstract int Priority { get; }

    public string? Locate()
    {
        Logger.LogDebug("Locating uploader by locator. Priority: {priority}.", Priority);
        string? uploaderFullPath = GetUploaderFullPath();

        if (string.IsNullOrEmpty(uploaderFullPath))
        {
            Logger.LogDebug("Uploader can't be located.");
            return null;
        }

        if (FileService.Exists(uploaderFullPath!))
        {
            Logger.LogDebug("Uploader found: {filePath}", uploaderFullPath);
            return uploaderFullPath;
        }

        Logger.LogDebug("Uploader not found: {filePath}", uploaderFullPath);

        return null;
    }

    /// <summary>
    /// Gets uploader path. If the uploader path is not applicable to the locator, returns null.
    /// </summary>
    protected abstract string? GetUploaderFullPath();
}
