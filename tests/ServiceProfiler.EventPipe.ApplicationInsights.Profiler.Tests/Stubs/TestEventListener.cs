using System;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    internal class TestEventListener : EventListener
    {
        public TestEventListener(Action<EventSource> onEventSourceCreated)
        {
            _onEventSourceCreated = onEventSourceCreated;
            _ctorWaitHandle.Set();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            base.OnEventSourceCreated(eventSource);
            Task.Run(() =>
            {
                _ctorWaitHandle.WaitOne();
                _onEventSourceCreated?.Invoke(eventSource);
            });
        }

        private readonly EventWaitHandle _ctorWaitHandle = new ManualResetEvent(false);
        private readonly Action<EventSource> _onEventSourceCreated;

        public override void Dispose()
        {
            _ctorWaitHandle.Dispose();
            base.Dispose();
        }
    }
}
