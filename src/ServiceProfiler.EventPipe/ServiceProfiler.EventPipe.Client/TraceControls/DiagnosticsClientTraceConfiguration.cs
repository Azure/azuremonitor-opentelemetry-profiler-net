#nullable enable

using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Microsoft.ApplicationInsights.Profiler.Core.TraceControls;

internal class DiagnosticsClientTraceConfiguration : DiagnosticsClientTraceConfigurationBase
{
    public DiagnosticsClientTraceConfiguration(
        IOptions<UserConfiguration> userConfiguration,
        ILogger<DiagnosticsClientTraceConfiguration> logger) : base(userConfiguration, logger)
    {
    }

    protected override IEnumerable<EventPipeProvider> AppendEventPipeProviders()
    {
        // Microsoft-ApplicationInsights-DataRelay
        yield return new EventPipeProvider(ApplicationInsightsDataRelayEventSource.EventSourceName, EventLevel.Verbose, keywords: 0xffffffff, arguments: null);
    }
}
