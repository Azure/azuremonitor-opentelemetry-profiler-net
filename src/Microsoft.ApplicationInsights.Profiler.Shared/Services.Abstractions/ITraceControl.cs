//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface ITraceControl
{
    /// <summary>
    /// Disables the current profiler session.
    /// </summary>
    /// <exception cref="System.TimeoutException">Throws when timed out fetching the semaphore of the operation.</exception>
    Task DisableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Enables a profiler session.
    /// </summary>
    /// <remark>
    /// Only 1 profiler session is supported at a time.
    /// </remark>
    Task EnableAsync(string traceFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the session start time
    /// </summary>
    DateTime? SessionStartUTC { get; }
}