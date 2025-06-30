//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Core.Contracts
{
    /// <summary>
    /// Enums for endpoint names.
    /// IngestionEndpoint = 0;
    /// LiveEndpoint = 1;
    /// ProfilerEndpoint = 2;
    /// SnapshotEndpoint = 3;
    /// </summary>
    /// <remark>
    /// SnapshotEndpoint is not exposed on purpose.
    /// It is not used by profiler, and the stamps are combined and they are the same.
    /// </remark>
    internal enum EndpointName
    {
        IngestionEndpoint = 0,
        LiveEndpoint,
        ProfilerEndpoint,
    }
}
