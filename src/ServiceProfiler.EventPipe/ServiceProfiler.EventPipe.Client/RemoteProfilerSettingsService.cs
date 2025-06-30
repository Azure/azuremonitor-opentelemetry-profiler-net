//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent.FrontendClient;

namespace Microsoft.ApplicationInsights.Profiler.Core;

// TODO: Make this a hosted service running on the background.
internal sealed class RemoteProfilerSettingsService : RemoteSettingsServiceBase
{
    private readonly IServiceProvider _serviceProvider;

    public RemoteProfilerSettingsService(
        IProfilerFrontendClient frontendClient,
        IOptions<UserConfiguration> userConfigurationOptions,
        ILogger<RemoteSettingsServiceBase> logger,
        IServiceProvider serviceProvider) : base(frontendClient, userConfigurationOptions, logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override IDisposable EnterInternalZone()
        => ActivatorUtilities.CreateInstance<DisposableSdkInternalOperationsMonitor>(_serviceProvider);
}
