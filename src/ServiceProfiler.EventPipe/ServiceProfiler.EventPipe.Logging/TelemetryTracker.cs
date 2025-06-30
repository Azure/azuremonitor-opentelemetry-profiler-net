//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.Common.Utilities;
using static System.Globalization.CultureInfo;

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging;

internal sealed class TelemetryTracker : IEventPipeTelemetryTracker
{
    private readonly IAppInsightsLogger _msAILogger;
    private string? _unhealthyReason;
#pragma warning disable IDE1006 // Naming Styles: nameof(HeartbeatInterval) has been used in telemetry.
    internal static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(30);
#pragma warning restore IDE1006 // Naming Styles

    public TelemetryTracker(
        IAppInsightsLogger msAILogger)
    {
        _msAILogger = msAILogger ?? throw new ArgumentNullException(nameof(msAILogger));
        _msAILogger.SetCommonProperty(Constants.StartTimeUTC, DateTimeOffset.UtcNow.ToString("o", InvariantCulture));
        _msAILogger.SetCommonProperty(Constants.OS, RuntimeInformation.OSDescription);
        _msAILogger.SetCommonProperty(Constants.Runtime, RuntimeInformation.FrameworkDescription);
        _msAILogger.SetCommonProperty(Constants.SessionId, EnvironmentUtilities.CreateSessionId());
        string componentVersion = EnvironmentUtilities.ExecutingAssemblyInformationalVersion;
        _msAILogger.SetCommonProperty(Constants.ComponentVersion, componentVersion);
        if (CurrentProcessUtilities.TryGetId(out int? pid))
        {
            _msAILogger.SetCommonProperty(Constants.ProcessId, pid!.Value.ToString(InvariantCulture));
        }
        _msAILogger.SetCommonProperty(Constants.CloudRoleInstance, EnvironmentUtilities.HashedMachineNameWithoutSiteName);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
        => StartHeartbeatAsync(cancellationToken);

    /// <inheritdoc />
    public void SetCustomerAppInfo(Guid appId)
    {
        _msAILogger.SetCommonProperty(Constants.AuthenticatedUserId, appId.ToString());
    }

    /// <inheritdoc />
    public void SetUnhealthy(string reason) => _unhealthyReason = reason;

    /// <inheritdoc />
    public void SetHealthy() => _unhealthyReason = null;

    private async Task StartHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var properties = new Dictionary<string, string>
            {
                [nameof(HeartbeatInterval)] = HeartbeatInterval.ToString(null, InvariantCulture)
            };

            if (_unhealthyReason != null)
            {
                properties.Add("Unhealthy", _unhealthyReason);
            }

            _msAILogger.TrackEvent(Constants.Heartbeat,
                properties: properties);

            await Task.Delay(HeartbeatInterval, cancellationToken).ConfigureAwait(false);
        }
    }
}
