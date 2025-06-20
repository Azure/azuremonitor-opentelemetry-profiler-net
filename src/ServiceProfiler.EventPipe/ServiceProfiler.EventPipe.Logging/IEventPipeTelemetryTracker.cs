//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging;

internal interface IEventPipeTelemetryTracker
{
    /// <summary>
    /// Start the tracking service.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Sets the app id.
    /// </summary>
    void SetCustomerAppInfo(Guid appId);

    /// <summary>
    /// Indicate that Service Profiler is unhealthy.
    /// </summary>
    /// <param name="reason">The reason for being unhealthy.</param>
    void SetUnhealthy(string reason);

    /// <summary>
    /// Indicate that Service Profiler is healthy again.
    /// </summary>
    void SetHealthy();
}
