using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class UploaderLocatorTests : TestsBase
    {
        private const string UploaderLocatorEnvironmentVariablePattern = "%SP_UPLOADER_PATH%";
        private const string UploaderAssemblyName = "Microsoft.ApplicationInsights.Profiler.Uploader.dll";

        [Fact]
        public void ShouldHaveMeaningfulPriority()
        {
            Mock<IFile> filesMock = new();
            Mock<IEnvironment> environmentsMock = new();
            Mock<IZipFile> zipFileMock = new();

            IPrioritizedUploaderLocator envUploaderLocator = new UploaderLocatorByEnvironmentVariable(filesMock.Object, environmentsMock.Object, GetLogger<UploaderLocatorByEnvironmentVariable>());
            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();

            IPrioritizedUploaderLocator userCacheLocator = new UploaderLocatorInUserCache(userCacheManagerMock.Object, filesMock.Object, GetLogger<UploaderLocatorInUserCache>());
            Mock<IProfilerCoreAssemblyInfo> profilerCoreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            IPrioritizedUploaderLocator unzipUploaderLocator = new UploaderLocatorByUnzipping(userCacheManagerMock.Object, profilerCoreAssemblyInfoMock.Object, filesMock.Object, zipFileMock.Object, GetLogger<UploaderLocatorByUnzipping>());

            // The smaller the value of the priority, the higher the priority.
            Assert.True(envUploaderLocator.Priority < userCacheLocator.Priority);
            Assert.True(userCacheLocator.Priority < unzipUploaderLocator.Priority);
        }

        [Fact]
        public void ShouldFoundUploaderByEnvironmentVariableWhenFileExists()
        {
            string existsFilePath = @"filePath";

            Mock<IFile> filesMock = new();
            Mock<IEnvironment> environmentMock = new();

            // Only when the file path matches, returns true.
            filesMock.Setup(f => f.Exists(It.Is<string>(value => string.Equals(existsFilePath, value, StringComparison.OrdinalIgnoreCase)))).Returns(true);
            // Returns correct file path.
            environmentMock.Setup(e => e.ExpandEnvironmentVariables(It.IsAny<string>())).Returns(existsFilePath);

            IPrioritizedUploaderLocator target = new UploaderLocatorByEnvironmentVariable(filesMock.Object, environmentMock.Object, GetLogger<UploaderLocatorByEnvironmentVariable>());
            var actual = target.Locate();
            Assert.Equal(existsFilePath, actual);
        }

        [Fact]
        public void ShouldNotFoundUploaderWhenEnvironmentVariableIsNotSet()
        {
            string existsFilePath = @"filePath";

            Mock<IFile> filesMock = new();
            Mock<IEnvironment> environmentMock = new();

            // Only when the file path matches, returns true.
            filesMock.Setup(f => f.Exists(It.Is<string>(value => string.Equals(existsFilePath, value, StringComparison.OrdinalIgnoreCase)))).Returns(true);
            // Environment variable doesn't return proper file path.
            environmentMock.Setup(e => e.ExpandEnvironmentVariables(It.IsAny<string>())).Returns("wrong-path");

            IPrioritizedUploaderLocator target = new UploaderLocatorByEnvironmentVariable(filesMock.Object, environmentMock.Object, GetLogger<UploaderLocatorByEnvironmentVariable>());
            var actual = target.Locate();

            Assert.Null(actual);
        }

        [Fact]
        public void ShouldReturnByUserCacheUploaderLocatorWhenExists()
        {
            const string uploaderPath = @"/tmp";
            string expected = Path.GetFullPath(Path.Combine(uploaderPath, UploaderAssemblyName));

            Mock<IFile> filesMock = new Mock<IFile>();
            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();

            // Always assume file exists.
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));

            IPrioritizedUploaderLocator userCacheLocator = new UploaderLocatorInUserCache(userCacheManagerMock.Object, filesMock.Object, GetLogger<UploaderLocatorInUserCache>());
            string actual = userCacheLocator.Locate();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldReturnNullByUserCacheUploaderLocatorWhenFileDoesNotExists()
        {
            const string uploaderPath = @"/tmp";
            string expected = Path.GetFullPath(Path.Combine(uploaderPath, UploaderAssemblyName));

            Mock<IFile> filesMock = new Mock<IFile>();
            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();

            // Always assume file does not exist.
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));

            IPrioritizedUploaderLocator userCacheLocator = new UploaderLocatorInUserCache(userCacheManagerMock.Object, filesMock.Object, GetLogger<UploaderLocatorInUserCache>());
            string actual = userCacheLocator.Locate();

            Assert.Null(actual);
        }

        [Fact]
        public void ShouldUseTheGivenPathInUserCacheUploaderLocator()
        {
            const string uploaderPath = @"/tmp";
            string expected = Path.GetFullPath(Path.Combine(uploaderPath, UploaderAssemblyName));

            Mock<IFile> filesMock = new Mock<IFile>();
            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();

            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));
            filesMock.Setup(f => f.Exists(It.Is<string>(value => string.Equals(expected, Path.GetFullPath(value), StringComparison.OrdinalIgnoreCase)))).Returns(true);

            IPrioritizedUploaderLocator userCacheLocator = new UploaderLocatorInUserCache(userCacheManagerMock.Object, filesMock.Object, GetLogger<UploaderLocatorInUserCache>());
            string actual = userCacheLocator.Locate();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldReturnExtractedPathByUnzippingUploaderLocator()
        {
            const string userCache = @"/tmp";
            const string uploaderPath = @"/tmp/uploader";
            string expected = Path.GetFullPath(Path.Combine(uploaderPath, UploaderAssemblyName));

            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();
            Mock<IFile> filesMock = new();
            Mock<IZipFile> zipFileMock = new();

            // Assuming file always exists.
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            userCacheManagerMock.Setup(uc => uc.UserCacheDirectory).Returns(new DirectoryInfo(userCache));
            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));

            bool extractRan = false;
            zipFileMock.Setup(z => z.ExtractToDirectory(It.IsAny<string>(), It.IsAny<string>())).Callback(() =>
            {
                extractRan = true;
            });

            Mock<IProfilerCoreAssemblyInfo> coreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            coreAssemblyInfoMock.Setup(a => a.Directory).Returns(new DirectoryInfo("."));
            IPrioritizedUploaderLocator target = new UploaderLocatorByUnzipping(
                userCacheManagerMock.Object,
                coreAssemblyInfoMock.Object,
                filesMock.Object,
                zipFileMock.Object,
                GetLogger<UploaderLocatorByUnzipping>());

            string actual = target.Locate();
            Assert.Equal(expected, actual);
            Assert.True(extractRan);
        }

        [Fact]
        public void ShouldReturnNullByUnzippingUploaderLocatorWhenZipFileNotExists()
        {
            const string userCache = @"/tmp";
            const string uploaderPath = @"/tmp/uploader";

            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();
            Mock<IFile> filesMock = new();
            Mock<IZipFile> zipFileMock = new();

            // Assuming file always absent.
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);

            userCacheManagerMock.Setup(uc => uc.UserCacheDirectory).Returns(new DirectoryInfo(userCache));
            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));

            bool extractRan = false;
            zipFileMock.Setup(z => z.ExtractToDirectory(It.IsAny<string>(), It.IsAny<string>())).Callback(() =>
            {
                extractRan = true;
            });

            Mock<IProfilerCoreAssemblyInfo> coreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            coreAssemblyInfoMock.Setup(a => a.Directory).Returns(new DirectoryInfo("."));
            IPrioritizedUploaderLocator target = new UploaderLocatorByUnzipping(
                userCacheManagerMock.Object,
                coreAssemblyInfoMock.Object,
                filesMock.Object,
                zipFileMock.Object,
                GetLogger<UploaderLocatorByUnzipping>());

            string actual = target.Locate();
            Assert.Null(actual);
            Assert.False(extractRan);
        }

        [Fact]
        public void ShouldSearchExpectedLocationsByUnzippingUploader()
        {
            const string userCache = @"/tmp";
            const string uploaderPath = @"/tmp/uploader";

            Mock<IFile> filesMock = new();
            Mock<IUserCacheManager> userCacheManagerMock = new Mock<IUserCacheManager>();
            Mock<IProfilerCoreAssemblyInfo> coreAssemblyInfoMock = new Mock<IProfilerCoreAssemblyInfo>();
            Mock<IZipFile> zipFileMock = new();

            // Assuming file exists
            filesMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            userCacheManagerMock.Setup(uc => uc.UserCacheDirectory).Returns(new DirectoryInfo(userCache));
            userCacheManagerMock.Setup(uc => uc.UploaderDirectory).Returns(new DirectoryInfo(uploaderPath));
            coreAssemblyInfoMock.Setup(a => a.Directory).Returns(new DirectoryInfo("."));

            UploaderLocatorByUnzipping target = new UploaderLocatorByUnzipping(userCacheManagerMock.Object, coreAssemblyInfoMock.Object, filesMock.Object, zipFileMock.Object, GetLogger<UploaderLocatorByUnzipping>());
            IEnumerable<string> searchPaths = target.GetZipDirectories();

            string[] expected = new string[] {
                Path.Combine(Directory.GetCurrentDirectory(),"ServiceProfiler"), // [AssemblyFolder]/ServiceProfiler
                Directory.GetCurrentDirectory(),    // [AssemblyFolder]
            };

            Assert.Equal(expected, searchPaths);
        }
    }
}
