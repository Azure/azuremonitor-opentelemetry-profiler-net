using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class ThreadUtilitiesTests
    {
        [Fact]
        public async Task ShouldTimeout()
        {
            Exception ex = await Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await ThreadUtilities.Instance.Value.CallWithTimeoutAsync(
                    action: () =>
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    },
                    timeout: TimeSpan.FromSeconds(1));
            });

            Assert.Equal("Call didn't finish in time.", ex.Message);
        }

        [Fact]
        public async Task ShouldNotTimeout()
        {
            Task task = ThreadUtilities.Instance.Value.CallWithTimeoutAsync(
                action: () =>
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                },
                timeout: TimeSpan.FromSeconds(2));
            await task;

            Assert.True(task.IsCompleted);
        }
        
        [Fact]
        public async Task ShouldNotSwallowExceptions()
        {
            Exception ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ThreadUtilities.Instance.Value.CallWithTimeoutAsync(
                    action: () =>
                    {
                        throw new InvalidOperationException("Test exception");
                    },
                    timeout: TimeSpan.FromSeconds(2));
            });

            Assert.Equal("Test exception", ex.Message);
        }
    }
}