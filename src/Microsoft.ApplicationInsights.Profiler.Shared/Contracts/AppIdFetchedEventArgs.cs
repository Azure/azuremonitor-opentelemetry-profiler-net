//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal sealed class AppIdFetchedEventArgs : EventArgs
{
    public Guid AppId { get; }
    public AppIdFetchedEventArgs(Guid appId)
    {
        AppId = appId;
    }
}