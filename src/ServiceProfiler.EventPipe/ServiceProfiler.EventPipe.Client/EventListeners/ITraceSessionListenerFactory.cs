//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal interface ITraceSessionListenerFactory
    {
        IEnumerable<ITraceSessionListener> CreateTraceSessionListeners();
    }
}
