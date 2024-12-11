//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// A service that provides a cloud role name info.
/// </summary>
internal interface IRoleNameSource
{
    /// <summary>
    /// Gets the cloud role name of a telemetry instance.
    /// </summary>
    string CloudRoleName { get; }
}
