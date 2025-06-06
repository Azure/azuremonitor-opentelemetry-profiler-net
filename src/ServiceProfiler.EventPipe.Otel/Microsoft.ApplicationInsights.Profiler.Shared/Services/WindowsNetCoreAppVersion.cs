//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class WindowsNetCoreAppVersion : INetCoreAppVersion
{
    public string NetCore2_1 => "4.6.26614.01";

    public string NetCore2_2 => "4.6.27110.04";
}
