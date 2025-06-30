//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal interface ITraceValidatorFactory
    {
        ITraceValidator Create(string traceFilePath);
    }
}