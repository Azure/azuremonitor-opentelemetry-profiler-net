//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// A service to provide functions like those in <see cref="System.IO.File" /> class.
/// </summary>
internal interface IFile
{
    /// <summary>
    /// Check if the file in the given path exists or not.
    /// </summary>
    /// <param name="path">Target for existence testing.</param>
    /// <returns>Returns true when the target exists. False otherwise.</returns>
    bool Exists(string path);
}
