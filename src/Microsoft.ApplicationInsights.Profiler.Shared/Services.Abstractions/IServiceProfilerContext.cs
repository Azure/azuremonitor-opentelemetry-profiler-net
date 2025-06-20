// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using ServiceProfiler.Common.Utilities;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IServiceProfilerContext
{
    ConnectionString ConnectionString { get; }

    Guid AppInsightsInstrumentationKey { get; }

    string MachineName { get; }

    Uri StampFrontendEndpointUrl { get; }
}
