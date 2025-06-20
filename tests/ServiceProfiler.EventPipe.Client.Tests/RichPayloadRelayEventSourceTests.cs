using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class RichPayloadRelayEventSourceTests
    {
        [Fact]
        public async Task ShouldUseProperEventSourceNameAsync()
        {
            string expectedEventSourceName = "Microsoft-ApplicationInsights-DataRelay";
            ApplicationInsightsDataRelayEventSource.Log.RequestStart("id",
                "name",
                DateTimeOffset.UtcNow.AddSeconds(-2).UtcTicks,
                DateTimeOffset.UtcNow.UtcTicks,
                "requestId",
                "operationName",
                "machineName",
                "operationId");
            TaskCompletionSource<bool> _validationFinished = new TaskCompletionSource<bool>();

            using (var listener = new TestEventListener(eventSource =>
            {
                if (string.Equals(eventSource.Name, ApplicationInsightsDataRelayEventSource.EventSourceName, StringComparison.Ordinal))
                {
                    Assert.Equal(expectedEventSourceName, eventSource.Name);
                    _validationFinished.SetResult(true);
                }
            }))
            {

                Task executed = await Task.WhenAny(
                    _validationFinished.Task,
                    Task.Delay(TimeSpan.FromSeconds(2)));
                Assert.Equal(executed, _validationFinished.Task);
            }
        }

        [Fact]
        public async Task ShouldUseProperEventSourceIdAsync()
        {
            const string expectedGuidString = "8206c581-c6a3-550a-af53-6e0421740cbe";
            ApplicationInsightsDataRelayEventSource.Log.RequestStart("id",
                "name",
                DateTimeOffset.UtcNow.AddSeconds(-2).UtcTicks,
                DateTimeOffset.UtcNow.UtcTicks,
                "requestId",
                "operationName",
                "machineName",
                "operationId");
            TaskCompletionSource<bool> _validationFinished = new TaskCompletionSource<bool>();

            using (var listener = new TestEventListener(eventSource =>
            {
                if (string.Equals(eventSource.Name, ApplicationInsightsDataRelayEventSource.EventSourceName, StringComparison.Ordinal))
                {
                    string actual = eventSource.Guid.ToString("d");
                    Assert.Equal(expectedGuidString, actual);
                    _validationFinished.SetResult(true);
                }
            }))
            {
                Task executed = await Task.WhenAny(
                    _validationFinished.Task,
                    Task.Delay(TimeSpan.FromSeconds(2)));

                Assert.Equal(expectedGuidString, ApplicationInsightsDataRelayEventSource.EventSourceGuidString);
                Assert.Equal(executed, _validationFinished.Task);
            }
        }

        [Fact]
        public void ShouldUseNameGuidConvention()
        {
            // Event source name and its guid should always be matched:
            // Refer: https://blogs.msdn.microsoft.com/dcook/2015/09/08/etw-provider-names-and-guids/

            // The event source name shall never change.
            Assert.Equal("Microsoft-ApplicationInsights-DataRelay", ApplicationInsightsDataRelayEventSource.EventSourceName);

            // The guid shall never change.
            Guid expected = new("8206c581-c6a3-550a-af53-6e0421740cbe");
            Guid actual = Guid.Parse(ApplicationInsightsDataRelayEventSource.EventSourceGuidString);
            Assert.Equal(expected, actual);
        }
    }
}
