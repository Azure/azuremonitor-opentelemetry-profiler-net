//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Core;
internal interface IAppInsightsSinks
{
    /// <summary>
    /// Logs an information.
    /// </summary>
    /// <param name="message">The message.</param>
    void LogInformation(string message);
}
