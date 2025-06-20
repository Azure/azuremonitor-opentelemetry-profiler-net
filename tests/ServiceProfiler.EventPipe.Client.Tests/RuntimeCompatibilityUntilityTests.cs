using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class RuntimeCompatibilityUtilityTests
    {
        [Theory]
        [InlineData("3.0.0")]  // .NET Core 3.0
        [InlineData("3.1.0")]  // .NET Core 3.1
        [InlineData("3.12.0")] // Any 3.x should have been supported.
        [InlineData("5.0.0")] // .NET 5
        [InlineData("12.1.0")] // .NET X
        public void ShouldSupportThisMajorVersion(string version)
        {
            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(vp => vp.RuntimeVersion).Returns(new Version(version));
            var loggerMock = new Mock<ILogger<RuntimeCompatibilityUtility>>();
            RuntimeCompatibilityUtility target = new RuntimeCompatibilityUtility(
                new WindowsNetCoreAppVersion(),
                versionProviderMock.Object,
                loggerMock.Object);

            var actual = target.IsCompatible();

            Assert.True(actual.compatible);
            Assert.Equal("Good major version. Pass Runtime Compatibility test.", actual.reason);
        }

        [Theory]
        [InlineData("4.6.27110.04")]  // Minimal supported .NET Core 2.2 version. This is an edge case.
        [InlineData("4.7.11111.01")]  // Anything greater than the bound.
        public void ShouldSupportTheseOlderVersions(string version)
        {
            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(vp => vp.RuntimeVersion).Returns(new Version(version));
            var loggerMock = new Mock<ILogger<RuntimeCompatibilityUtility>>();
            RuntimeCompatibilityUtility target = new RuntimeCompatibilityUtility(
                new WindowsNetCoreAppVersion(),
                versionProviderMock.Object,
                loggerMock.Object);

            var actual = target.IsCompatible();

            Assert.True(actual.compatible);
            Assert.Equal("Pass Runtime Compatibility test.", actual.reason);
        }

        [Theory]
        [InlineData("4.6.27110.03")] // Lower than supported .NET Core 2.2 version.
        [InlineData("2.0.0")] // .NET Core 2.0 isn't supported.
        [InlineData("1.0.0")] // .NET Core 1.0 isn't supported.
        public void ShouldNotSupportThisVersion(string version)
        {
            var versionProviderMock = new Mock<IVersionProvider>();
            versionProviderMock.Setup(vp => vp.RuntimeVersion).Returns(new Version(version));
            var loggerMock = new Mock<ILogger<RuntimeCompatibilityUtility>>();
            RuntimeCompatibilityUtility target = new RuntimeCompatibilityUtility(
                new WindowsNetCoreAppVersion(),
                versionProviderMock.Object,
                loggerMock.Object);

            var actual = target.IsCompatible();

            Assert.False(actual.compatible);
        }
    }
}