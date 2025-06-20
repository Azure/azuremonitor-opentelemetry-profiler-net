using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class SampleActivityContainerTests
    {
        [Fact]
        public void ShouldAllowConcurrentDuringArrayExpansion()
        {
            // This is a regression test. See https://github.com/microsoft/ApplicationInsights-Profiler-AspNetCore/issues/182 for details.
            // Given 10000 iterations, it almost always throw IndexOutOfRange exception before the bug fixing.
            for (int i = 0; i < 10000; i++)
            {
                double[] durations = new double[] { 6.6419, 54.3629, 1.9237, 106.432, 1.5464, 6.6419, 54.3629, 1.9237, 106.432, 1.5464, 6.6419, 54.3629, 1.9237, 106.432, 1.5464, 6.6419, 54.3629, 1.9237, 106.432, 1.5464, 6.6419, 54.3629, 1.9237, 106.432, 1.5464, 6.6419, 54.3629, 1.9237, 106.432, 1.5464 };

                ILogger<SampleActivityContainer> logger = new NullLogger<SampleActivityContainer>();
                SampleActivityContainer sampleActivityContainer = new SampleActivityContainer(logger);

                Parallel.ForEach(durations, new ParallelOptions(), (duration, state, index) =>
                {
                    sampleActivityContainer.AddSample(new SampleActivity() { OperationName = nameof(ShouldAllowConcurrentDuringArrayExpansion), Duration = TimeSpan.FromMilliseconds(duration) });
                });
            }
        }
    }
}
