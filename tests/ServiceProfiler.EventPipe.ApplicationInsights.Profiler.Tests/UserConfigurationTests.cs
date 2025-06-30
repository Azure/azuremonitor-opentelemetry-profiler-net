using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class UserConfigurationTests
    {
        [Fact]
        public void ShouldHaveMeaningfulDefaultValues()
        {
            // Whenever a property is added or removed, this needs to be updated.
            const int expectedPropertyNum = 23;
            int propertyCount = typeof(UserConfiguration).GetTypeInfo().DeclaredProperties.Count();
            int propertyOffBaseClassCount = typeof(UserConfigurationBase).GetTypeInfo().DeclaredProperties.Count();

            Assert.Equal(expectedPropertyNum, propertyCount + propertyOffBaseClassCount);

            // Same count of properties as expectedPropertyNum should be checked for default value.
            UserConfiguration configuration = new UserConfiguration();
            Assert.Equal(250, configuration.BufferSizeInMB);
            Assert.Equal(TimeSpan.FromSeconds(30), configuration.Duration);
            Assert.Equal(TimeSpan.Zero, configuration.InitialDelay);
            Assert.Null(configuration.Endpoint);
            Assert.False(configuration.ProvideAnonymousTelemetry);
            Assert.False(configuration.IsDisabled);
            Assert.False(configuration.IsSkipCompatibilityTest);
            Assert.False(configuration.PreserveTraceFile);
            Assert.False(configuration.SkipEndpointCertificateValidation);
            Assert.Equal(UploadMode.OnSuccess, configuration.UploadMode);
            Assert.Equal(TimeSpan.FromSeconds(5), configuration.ConfigurationUpdateFrequency);
            Assert.Equal(0.01f, configuration.RandomProfilingOverhead);
            Assert.Equal(80.0F, configuration.CPUTriggerThreshold);
            Assert.Equal(80.0F, configuration.MemoryTriggerThreshold);
            Assert.False(configuration.StandaloneMode);
            Assert.Equal(Path.GetTempPath(), configuration.LocalCacheFolder);
            Assert.False(configuration.AllowsCrash);
            Assert.NotNull(configuration.NamedPipe);
            Assert.Equal(TimeSpan.FromSeconds(30), configuration.NamedPipe.ConnectionTimeout);
            Assert.Equal(TimeSpan.FromSeconds(14400), configuration.MemoryTriggerCooldown);
            Assert.Equal(TimeSpan.FromSeconds(14400), configuration.CPUTriggerCooldown);
            Assert.Equal("Production", configuration.UploaderEnvironment);
            Assert.Empty(configuration.CustomEventPipeProviders);

            Assert.NotNull(configuration.TraceScavenger);
            Assert.Equal(TimeSpan.Zero, configuration.TraceScavenger.InitialDelay);
            Assert.Equal(TimeSpan.FromHours(1), configuration.TraceScavenger.Interval);
            Assert.Equal(TimeSpan.FromMinutes(10), configuration.TraceScavenger.GracePeriod);
        }
    }
}
