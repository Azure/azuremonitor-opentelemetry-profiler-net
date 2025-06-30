namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// A service provides functions similar to <see cref="System.IO.Compression.ZipFile" />.
/// </summary>
internal interface IZipFile
{
    /// <summary>
    /// Extracts all the files in the specified zip archive to a directory on the file system.
    /// </summary>
    /// <param name="sourceArchiveFileName">The path to the archive that is to be extracted.</param>
    /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files, specified as a relative or absolute path. A relative path is interpreted as relative to the current working directory.</param>
    void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName);
}
