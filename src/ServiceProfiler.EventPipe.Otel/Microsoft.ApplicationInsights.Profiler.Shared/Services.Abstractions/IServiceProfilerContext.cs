// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IServiceProfilerContext
{
    // Guid AppInsightsAppId { get; }

    string? ConnectionString { get; }

    Guid AppInsightsInstrumentationKey { get; }
    bool HasAppInsightsInstrumentationKey { get; }

    string MachineName { get; }
    // CancellationTokenSource ServiceProfilerCancellationTokenSource { get; }
    Uri StampFrontendEndpointUrl { get; }

    // event EventHandler<AppIdFetchedEventArgs> AppIdFetched;

    // Task<Guid> GetAppInsightsAppIdAsync();
    // Task<Guid> GetAppInsightsAppIdAsync(CancellationToken cancellationToken);

    // void OnAppIdFetched(Guid appId);
}
