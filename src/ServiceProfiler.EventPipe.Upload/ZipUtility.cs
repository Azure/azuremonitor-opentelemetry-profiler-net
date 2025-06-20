//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal sealed class ZipUtility : IZipUtility
    {
        private readonly ILogger<ZipUtility> _logger;

        public ZipUtility(ILogger<ZipUtility> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Zip netperf file, return the zipped file path.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public string ZipFile(string source, string zippedFileExtension = ".etl.zip", IEnumerable<string>? additionalFiles = null)
        {
            FileInfo sourceFileInfo = new FileInfo(source);
            if (!sourceFileInfo.Exists)
            {
                throw new FileNotFoundException($"File not found: {source}");
            }

            DirectoryInfo baseDirectoryInfo = sourceFileInfo.Directory ?? throw new InvalidOperationException("Source file must have a directory.");
            string workingDirectoryName = Path.GetRandomFileName();
            DirectoryInfo workingDirectoryInfo = baseDirectoryInfo.CreateSubdirectory(workingDirectoryName);

            string originalFileExtension = sourceFileInfo.Extension;
            string etlFolderName = Guid.NewGuid().ToString("D").ToUpperInvariant();
            DirectoryInfo etlFolderInfo = workingDirectoryInfo.CreateSubdirectory(etlFolderName);
            string etlFileName = $"{Guid.NewGuid().ToString("D").ToUpperInvariant()}";
            etlFileName = Path.ChangeExtension(etlFileName, originalFileExtension);
            File.Move(source, Path.Combine(etlFolderInfo.FullName, etlFileName));

            if (additionalFiles != null && additionalFiles.Any())
            {
                _logger.LogTrace("Additional file handling:");
                _logger.LogTrace("Creating additional directory:");
                DirectoryInfo additionalFolderInfo = workingDirectoryInfo.CreateSubdirectory(@"Additional");
                foreach (string additionalFilePath in additionalFiles)
                {
                    if (string.IsNullOrEmpty(additionalFilePath) || !File.Exists(additionalFilePath))
                    {
                        continue;
                    }

                    _logger.LogTrace(additionalFilePath);
                    string fileNameWithExtension = Path.GetFileName(additionalFilePath);
                    _logger.LogDebug("Get fileNameWithExt: {fileNameWithExtension}", fileNameWithExtension);
                    string targetFileFullPath = Path.Combine(additionalFolderInfo.FullName, fileNameWithExtension);
                    _logger.LogDebug("Move to {destination} from {source}", targetFileFullPath, additionalFilePath);
                    File.Move(additionalFilePath, targetFileFullPath);
                }
            }

            string destinationFilePath = Path.ChangeExtension(Path.Combine(baseDirectoryInfo.FullName, Path.GetFileNameWithoutExtension(source)), zippedFileExtension);
            _logger.LogDebug("Target zipped etl: {destination}", destinationFilePath);
            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }

            string workingFullPath = workingDirectoryInfo.FullName;
            System.IO.Compression.ZipFile.CreateFromDirectory(workingFullPath, destinationFilePath);

            // Deleting the working folder and all its sub-folders at best effort.
            try
            {
                _logger.LogDebug("Performance deleting on folder: {0}", workingFullPath);
                Directory.Delete(workingFullPath, true);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogWarning(ex, "Failed deleting working folder: {0}", workingFullPath);
            }

            return destinationFilePath;
        }
    }
}
