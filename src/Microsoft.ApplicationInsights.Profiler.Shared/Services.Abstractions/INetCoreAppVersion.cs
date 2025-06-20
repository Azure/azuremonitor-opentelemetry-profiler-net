//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface INetCoreAppVersion
{
    string NetCore2_1 { get; }
    string NetCore2_2 { get; }
}
