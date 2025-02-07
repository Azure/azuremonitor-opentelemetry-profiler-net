//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

#nullable disable

using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Samples;

internal class SampleActivityContainer
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, ValueBucketer<SampleActivityBucket, SampleActivity>> _operations;

    public SampleActivityContainer(ILogger<SampleActivityContainer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _operations = new ConcurrentDictionary<string, ValueBucketer<SampleActivityBucket, SampleActivity>>(StringComparer.OrdinalIgnoreCase);
    }

    public bool AddSample(SampleActivity activity)
    {
        ValueBucketer<SampleActivityBucket, SampleActivity> bucketer = _operations.GetOrAdd(activity.OperationName, (operationName) => new ValueBucketer<SampleActivityBucket, SampleActivity>());
        double activityDurationInMs = activity.Duration.TotalMilliseconds;
        _logger.LogDebug("Activity duration(ms): {duration}", activityDurationInMs);
        SampleActivityBucket bucket = bucketer.Get(activityDurationInMs);
        bucket.Add(activity);
        return true;
    }

    // Get all activities in the sample container.
    public IEnumerable<SampleActivity> GetActivities()
    {
        List<SampleActivity> samples = new List<SampleActivity>();
        foreach (var operation in _operations)
        {
            operation.Value.ForEach((value, bucket) =>
            {
                if (bucket != null && bucket.Samples != null)
                {
                    foreach (var sample in bucket.Samples)
                    {
                        samples.Add(sample);
                    }
                }

                return true;
            });
        }

        return samples;
    }
}
