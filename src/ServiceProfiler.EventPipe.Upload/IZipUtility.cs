//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    internal interface IZipUtility
    {
        /// <summary>
        /// Zip the given file, return the zipped file path.
        /// </summary>
        string ZipFile(string source, string zippedFileExtension = ".etl.zip", IEnumerable<string>? additionalFiles = null);
    }
}