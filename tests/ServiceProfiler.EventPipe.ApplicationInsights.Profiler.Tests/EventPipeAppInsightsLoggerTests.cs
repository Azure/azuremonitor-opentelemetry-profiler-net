//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using ServiceProfiler.Common.Utilities;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class EventPipeAppInsightsLoggerTests
    {
        private static readonly Guid TestIKey = Guid.Parse("8e321e78-834f-4397-96fd-297fa844d140");
        private const string RegionalIngestionEndpoint = "https://westus2-2.in.applicationinsights.azure.com/";

        [Fact]
        public void ShouldExposeConnectionStringWhenBuiltFromFullConfiguration()
        {
            Assert.True(ConnectionString.TryParse(
                $"InstrumentationKey={TestIKey};IngestionEndpoint={RegionalIngestionEndpoint}",
                out ConnectionString connectionString));

            using TelemetryConfiguration telemetryConfiguration = new()
            {
                ConnectionString = connectionString.ToString(),
            };

            using EventPipeAppInsightsLogger logger = new(telemetryConfiguration);

            Assert.NotNull(logger.ConnectionString);
            Assert.Equal(TestIKey, logger.ConnectionString!.InstrumentationKeyGuid);
        }

        [Fact]
        public void ShouldBuildConfigurationFromConnectionString()
        {
            using EventPipeAppInsightsLogger logger = new($"InstrumentationKey={TestIKey}");

            Assert.NotNull(logger.ConnectionString);
            Assert.Equal(TestIKey, logger.ConnectionString!.InstrumentationKeyGuid);
        }

        [Fact]
        public void ShouldThrowWhenConfigurationIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new EventPipeAppInsightsLogger((TelemetryConfiguration)null!));
        }
    }
}
