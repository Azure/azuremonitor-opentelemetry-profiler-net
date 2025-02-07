//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using OpenTelemetry;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.Orchestrations;

// TODO: Make this a hosted service running on the background.
internal sealed class RemoteSettingsService : RemoteSettingsServiceBase
{
    public RemoteSettingsService(
        IProfilerFrontendClient frontendClient,
        IOptions<UserConfigurationBase> userConfigurationOptions,
        ILogger<RemoteSettingsService> logger) : base(frontendClient, userConfigurationOptions, logger)
    {
    }

    protected override IDisposable EnterInternalZone() => SuppressInstrumentationScope.Begin();
}
