//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Diagnostics;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// Provides functions to call an external executable.
/// </summary>
public interface IOutOfProcCaller
{
    /// <summary>
    /// Execute the configured executable, wait until the process finished and return the exit code.
    /// </summary>
    int ExecuteAndWait(ProcessPriorityClass processPriorityClass = ProcessPriorityClass.Normal);
}
