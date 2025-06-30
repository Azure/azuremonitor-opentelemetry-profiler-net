using System.IO.Compression;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

/// <summary>
/// A simple implementation to <see cref="IZipFile" />.
/// </summary>
internal class SystemZipFile : IZipFile
{
    /// <inheritdoc />
    public void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName) => ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
}
