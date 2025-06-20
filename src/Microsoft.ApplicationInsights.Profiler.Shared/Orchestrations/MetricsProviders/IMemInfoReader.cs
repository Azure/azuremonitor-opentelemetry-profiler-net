//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.IO;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;

/// <summary>
/// A service to read mem info as a stream.
/// </summary>
internal interface IMemInfoReader
{
    /// <summary>
    /// Gets a stream for memory info from a file.
    /// </summary>
    /// <param name="filePath">The target file that contains the memory info. Implementations are supposed to provide the default path when possible.</param>
    /// <returns>A stream for the file.</returns>
    Stream Read(string? filePath = null);
}
