using System;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Core;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class UserCacheManagerTests
    {
        [Fact]
        public void ShouldTakeBasePathFromUserConfiguration()
        {
            const string userCachePath = @"/tmp/userDefinedFolder";
            const string VersionString = "2.1.3.4";

            UserConfiguration configuration = new UserConfiguration()
            {
                LocalCacheFolder = userCachePath,
            };
            Mock<IProfilerCoreAssemblyInfo> profilerCoreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            profilerCoreAssemblyInfoMock.Setup(a => a.Version).Returns(new Version(VersionString));
            
            UserCacheManager target = new UserCacheManager(Options.Create(configuration), profilerCoreAssemblyInfoMock.Object);
            DirectoryInfo cacheDirectoryInfo = target.UserCacheDirectory;
            DirectoryInfo uploaderPath = target.UploaderDirectory;

            string expectedCachePath = Path.GetFullPath(configuration.LocalCacheFolder);
            Assert.Equal(expectedCachePath, cacheDirectoryInfo.FullName);
        }

        [Fact]
        public void ShouldReturnUploaderPath()
        {
             const string userCachePath = @"/tmp/userDefinedFolder";
            const string VersionString = "2.1.3.4";

            UserConfiguration configuration = new UserConfiguration()
            {
                LocalCacheFolder = userCachePath,
            };
            Mock<IProfilerCoreAssemblyInfo> profilerCoreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            profilerCoreAssemblyInfoMock.Setup(a => a.Version).Returns(new Version(VersionString));
            
            UserCacheManager target = new UserCacheManager(Options.Create(configuration), profilerCoreAssemblyInfoMock.Object);
            DirectoryInfo cacheDirectoryInfo = target.UserCacheDirectory;
            DirectoryInfo uploaderPath = target.UploaderDirectory;

            string expectedUploaderPath = Path.GetFullPath(Path.Combine(configuration.LocalCacheFolder, "ServiceProfiler", VersionString, "Uploader"));
            Assert.Equal(expectedUploaderPath, target.UploaderDirectory.FullName);
        }
    }
}