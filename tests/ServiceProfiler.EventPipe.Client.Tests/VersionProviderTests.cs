using System;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class VersionProviderTests
    {
        // Refer to various version descriptions: https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#improved-net-core-version-apis
        [Theory]
        [InlineData("4.6.27415.71", ".NET Core 4.6.27415.71")]  // Traditional .NET Core version descriptions - before .NET Core 3.0
        [InlineData("3.0.0", ".NET Core 3.0.0-preview4-27615-11")] // .NET Core 3.x
        [InlineData("5.0.0", ".NET 5.0.0-rc.2.20475.5")] // .NET Core 5.x preview
        public void ShouldHandleVersionDescriptions(string versionString, string versionDescription)
        {
            var loggerMock = new Mock<ILogger<VersionProvider>>();

            Version expected = Version.Parse(versionString);
            var target = new VersionProvider(versionDescription, loggerMock.Object);
            Assert.Equal(expected, target.RuntimeVersion);
        }
    }
}