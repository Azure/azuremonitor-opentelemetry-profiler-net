using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Orchestration;
using System;
using System.Collections.Generic;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    internal class MockSchedulingPolicy : SchedulingPolicy
    {
        public MockSchedulingPolicy(
            IOptions<UserConfigurationBase> userConfig,
            IDelaySource delaySource,
            IExpirationPolicy expirationPolicy,
            ILogger<SchedulingPolicy> logger) :
            base(userConfig.Value.Duration,
                TimeSpan.FromHours(4),
                userConfig.Value.ConfigurationUpdateFrequency,
                delaySource,
                expirationPolicy,
                logger)
        {
        }

        public override string Source { get; } = nameof(MockSchedulingPolicy);

        public override IAsyncEnumerable<(TimeSpan duration, ProfilerAction action)> GetScheduleAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
