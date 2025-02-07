namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// A service to provide the full path of the uploader executable.
/// </summary>
internal interface IUploaderPathProvider
{
    /// <summary>
    /// Try gets a full path of the uploader. Returns true when uploader is located. Otherwise, false.
    /// </summary>
    bool TryGetUploaderFullPath(out string uploaderFullPath);

    /// <summary>
    /// Gets a full path to a existing uploader. Returns null if no uploader is located.
    /// </summary>
    /// <returns></returns>
    string GetUploaderFullPath();
}
