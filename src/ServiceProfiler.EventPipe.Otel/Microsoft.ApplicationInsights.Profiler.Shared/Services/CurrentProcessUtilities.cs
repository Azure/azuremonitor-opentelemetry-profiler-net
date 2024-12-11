// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Diagnostics;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

/// <summary>
/// A utility class to get the current process information.
/// This is a simple helper to manage the dispose of process object after use.
/// </summary>
internal static class CurrentProcessUtilities
{
    private static int? s_processId;

    /// <summary>
    /// Tries to get the unique identifier for the associated process. Returns true when succeeded. False when failed.
    /// </summary>
    /// <param name="processId">
    /// The process id of the current process when succeeded. Null when the id can't be fetched.
    /// </param>
    public static bool TryGetId(out int? processId)
    {
        try
        {
            processId = GetId();
            return true;
        }
        catch (InvalidOperationException)
        {
            // Refer to the doc: https://learn.microsoft.com/dotnet/api/system.diagnostics.process.id
            // The process's Id property has not been set.
            // or
            // There is no process associated with this Process object.

            // Ignore the exception since this is a best effort to get the process id.
            processId = null;
            return false;
        }
    }

    /// <summary>
    /// Tries to get the unique identifier for the associated process.
    /// </summary>
    /// <exception cref="InvalidOperationException">The process's Id property has not been set or there is no process associated with this Process object.</exception>
    /// <returns>The process id of the current process.</returns>
    public static int GetId()
    {
        if (s_processId is null)
        {
            using Process currentProcess = Process.GetCurrentProcess();
            s_processId = currentProcess.Id;
        }

        return s_processId.Value;
    }
}
