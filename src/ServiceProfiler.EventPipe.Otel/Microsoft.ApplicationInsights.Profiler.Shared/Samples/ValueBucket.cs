//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Samples;

internal abstract class ValueBucket<T>
{
    public int BucketIndex
    {
        get;
        set;
    }

    public abstract IEnumerable<T> Samples
    {
        get;
    }
}
