using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public sealed class TraceFileFixture : IDisposable
    {
        public TraceFileFixture()
        {
            DeleteNonZipFiles();

            // Extract trace files
            foreach (FileInfo zip in _deploymentFolder.EnumerateFiles("*.zip", SearchOption.TopDirectoryOnly))
            {
                ZipFile.ExtractToDirectory(zip.FullName, _deploymentFolder.FullName);
            }
        }

        public void Dispose()
        {
            DeleteNonZipFiles();
        }

        private void DeleteNonZipFiles()
        {
            List<FileInfo> deleteList = _deploymentFolder.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Where(IsNotZipFile).ToList();
            foreach (FileInfo toDelete in deleteList)
            {
                try
                {
                    File.Delete(toDelete.FullName);
                }
                catch (UnauthorizedAccessException)
                {
                    // TODO: Why?
                    // There is an intermediate issue when UnauthorizedAccessException is thrown.
                    // Do NOT understand why yet.
                }
            }
        }

        private bool IsNotZipFile(FileInfo fi)
        {
            return !string.Equals(".zip", fi.Extension, StringComparison.OrdinalIgnoreCase);
        }

        private readonly DirectoryInfo _deploymentFolder = new DirectoryInfo("TestDeployments");
    }
}
