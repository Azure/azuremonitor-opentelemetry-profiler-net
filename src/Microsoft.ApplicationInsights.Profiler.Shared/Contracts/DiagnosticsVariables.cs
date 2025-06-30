//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal static class DiagnosticsVariables
{
    public static IEnumerable<string> GetAllVariables()
    {
        yield return COMPlus_EnableDiagnostics;
        yield return DOTNET_EnableDiagnostics;
        yield return COMPlus_EnableDiagnostics_IPC;
        yield return DOTNET_EnableDiagnostics_IPC;
        yield return COMPlus_EnableDiagnostics_Profiler;
        yield return DOTNET_EnableDiagnostics_Profiler;
    }

    private const string COMPlus_EnableDiagnostics = "COMPlus_EnableDiagnostics";
    private const string DOTNET_EnableDiagnostics = "DOTNET_EnableDiagnostics";
    private const string COMPlus_EnableDiagnostics_IPC = "COMPlus_EnableDiagnostics_IPC";
    private const string DOTNET_EnableDiagnostics_IPC = "DOTNET_EnableDiagnostics_IPC";
    private const string COMPlus_EnableDiagnostics_Profiler = "COMPlus_EnableDiagnostics_Profiler";
    private const string DOTNET_EnableDiagnostics_Profiler = "DOTNET_EnableDiagnostics_Profiler";
}
