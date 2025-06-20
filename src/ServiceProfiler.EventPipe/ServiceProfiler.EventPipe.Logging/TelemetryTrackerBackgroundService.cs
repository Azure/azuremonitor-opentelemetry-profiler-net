// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.Extensions.Hosting;

namespace ServiceProfiler.EventPipe.Logging
{
    /// <summary>
    /// The background service to bootstrap the event pipe telemetry tracker.
    /// </summary>
    internal class TelemetryTrackerBackgroundService : BackgroundService
    {
        private readonly IEventPipeTelemetryTracker _tracker;

        public TelemetryTrackerBackgroundService(IEventPipeTelemetryTracker tracker)
        {
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Reduce chance to block the startup of the host. Refer to https://github.com/dotnet/runtime/issues/36063 for more details.
            await Task.Yield();
            await _tracker.StartAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}
