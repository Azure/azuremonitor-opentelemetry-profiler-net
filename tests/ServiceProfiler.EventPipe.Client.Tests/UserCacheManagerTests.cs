using System;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ServiceProfiler.EventPipe.Client.Tests.Stubs;
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

            UserConfigurationBase configuration = new UserConfigurationStub()
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

            UserConfigurationBase configuration = new UserConfigurationStub()
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