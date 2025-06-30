#nullable disable

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ServiceProfiler.Contract.DataContract.Utilities;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Samples;

internal class SampleActivityBucket : ValueBucket<SampleActivity>
{
    public override IEnumerable<SampleActivity> Samples
    {
        get
        {
            if (_sample != null)
            {
                yield return _sample;
            }
        }
    }

    public void Add(SampleActivity info)
    {
        double hashValue = GetHashValue(info.OperationId);
        if (hashValue < _currentHashValueMin)
        {
            _sample = info;
            _currentHashValueMin = hashValue;
        }
    }

    private double _currentHashValueMin = int.MaxValue;
    private SampleActivity _sample = null;

    private static double GetHashValue(string input)
    {
        double hashValue = HashingUtilities.Djb2(input) / ((double)int.MaxValue);
        return hashValue;
    }
}
