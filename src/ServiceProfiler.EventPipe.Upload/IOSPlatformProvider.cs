//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal interface IOSPlatformProvider
    {
        string GetOSPlatformDescription();
    }
}
