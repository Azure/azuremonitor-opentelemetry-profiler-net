using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ServiceProfiler.Orchestration;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class SchedulingPolicyTests : TestsBase
    {
        [Fact]
        public async Task SchedulingPolicyHasToBeRegisteredBeforeStarting()
        {
            IServiceCollection services = GetRichServiceCollection();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                ServiceProvider serviceProvider = services.BuildServiceProvider();

                SchedulingPolicy testSchedulingPolicy = serviceProvider.GetRequiredService<SchedulingPolicy>();

                PotentialCrashSchedulingPolicy target = new PotentialCrashSchedulingPolicy(
                    testSchedulingPolicy.ProfilingDuration,
                    testSchedulingPolicy.ProfilingCooldown,
                    testSchedulingPolicy.PollingInterval,
                    serviceProvider.GetRequiredService<IDelaySource>(),
                    serviceProvider.GetRequiredService<IExpirationPolicy>(),
                    NullLogger<SchedulingPolicy>.Instance);

                await Assert.ThrowsAsync<InvalidOperationException>(() => target.StartPolicyAsync(cts.Token));
            }
        }

        [Fact]
        public async Task SchedulingPolicyShouldNeverCrashAsync()
        {
            IServiceCollection services = GetRichServiceCollection();
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                ServiceProvider serviceProvider = services.BuildServiceProvider();

                SchedulingPolicy testSchedulingPolicy = serviceProvider.GetRequiredService<SchedulingPolicy>();

                PotentialCrashSchedulingPolicy target = new PotentialCrashSchedulingPolicy(
                    testSchedulingPolicy.ProfilingDuration,
                    testSchedulingPolicy.ProfilingCooldown,
                    testSchedulingPolicy.PollingInterval,
                    serviceProvider.GetRequiredService<IDelaySource>(),
                    serviceProvider.GetRequiredService<IExpirationPolicy>(),
                    serviceProvider.GetRequiredService<ILogger<SchedulingPolicy>>());

                var orchestrator = new Mock<IOrchestrator>();
                target.RegisterToOrchestrator(orchestrator.Object);

                // Dispatch the running of the Profiler to a different thread.
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await target.StartPolicyAsync(cts.Token);
                        taskCompletionSource.SetResult(true);
                    }
                    catch
                    {
                        taskCompletionSource.SetResult(false);
                    }
                });

                Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                Task t = await Task.WhenAny(taskCompletionSource.Task, timeoutTask);

                // Profiling is run for amount of time without crash.
                Assert.Same(t, timeoutTask);
            }
        }
    }

    class PotentialCrashSchedulingPolicy : SchedulingPolicy
    {
        public PotentialCrashSchedulingPolicy(
            TimeSpan profilingDuration,
            TimeSpan profilingCooldown,
            TimeSpan pollingFrequency,
            IDelaySource delaySource,
            IExpirationPolicy expirationPolicy,
            ILogger<SchedulingPolicy> logger) : base(profilingDuration, profilingCooldown, pollingFrequency, delaySource, expirationPolicy, logger)
        {
        }

        public override string Source => nameof(PotentialCrashSchedulingPolicy);

        public override Task<IEnumerable<(TimeSpan duration, ProfilerAction action)>> GetScheduleAsync()
        {
            throw new InvalidOperationException("Expected unexpected exception that might cause crash.");
        }
    }
}
