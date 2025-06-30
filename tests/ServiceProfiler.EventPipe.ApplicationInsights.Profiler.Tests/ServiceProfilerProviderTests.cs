//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceProfiler.Orchestration;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class ServiceProfilerProviderTests : TestsBase
    {
        [Theory()]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldCallUploaderUponStopTracing(bool isCustomerEventsSent)
        {
            TaskCompletionSource<bool> profilerStoppedTester = new();

            bool isUploaderCalled = false;
            IServiceProvider testServiceProvider = CreateServiceProvider(TimeSpan.FromSeconds(5), TimeSpan.Zero,
                uploaderExecuteCallback: () =>
                {
                    isUploaderCalled = true;
                    profilerStoppedTester.SetResult(true);
                });

            using (ServiceProfilerProvider target = testServiceProvider.GetRequiredService<ServiceProfilerProvider>())
            {
                SchedulingPolicy schedulingPolicy = testServiceProvider.GetRequiredService<SchedulingPolicy>();

                await target.StartServiceProfilerAsync(schedulingPolicy, default);
                if (isCustomerEventsSent)
                {
                    while (target.SessionListener == null) await Task.Delay(500);
                    ((TraceSessionListenerStub)target.SessionListener).AddSampleActivity();
                }

                await target.StopServiceProfilerAsync(schedulingPolicy, default);
                bool taskFinished = false;
                await Task.WhenAny(profilerStoppedTester.Task, Task.Delay(500));
                taskFinished = profilerStoppedTester.Task.IsCompleted;
                Assert.Equal(isCustomerEventsSent, taskFinished);
                Assert.Equal(isCustomerEventsSent, isUploaderCalled);
            }
        }

        #region Private
        private IServiceProvider CreateServiceProvider(
            TimeSpan duration,
            TimeSpan initialDelay,
            Action uploaderExecuteCallback = null,
            bool isIKeyNull = false)
        {
            IServiceCollection services = GetRichServiceCollection(duration, initialDelay);
            if (uploaderExecuteCallback != null)
            {
                _traceUploaderMock.Setup(u => u.UploadAsync(
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<string>()))
                    .Callback(uploaderExecuteCallback);
            }

            return services.BuildServiceProvider();
        }
        #endregion
    }
}
