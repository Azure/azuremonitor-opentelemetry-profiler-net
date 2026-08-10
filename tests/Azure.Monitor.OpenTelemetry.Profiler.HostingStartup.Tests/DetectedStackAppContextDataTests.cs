// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartupTests;

public class DetectedStackAppContextDataTests
{
    [Theory]
    [InlineData(TelemetryStack.OpenTelemetry, "otel")]
    [InlineData(TelemetryStack.LegacyApplicationInsights, "classic")]
    [InlineData(TelemetryStack.None, "")]
    [InlineData(TelemetryStack.AlreadyInstrumented, "")]
    [InlineData(TelemetryStack.AgentInstrumentedNoSdk, "")]
    [InlineData(TelemetryStack.AzureFunctionsPlatformHost, "")]
    internal void ToPayloadSubfolder_MapsStackToFolder(TelemetryStack stack, string expected)
    {
        Assert.Equal(expected, DetectedStackAppContextData.ToPayloadSubfolder(stack));
    }
}
