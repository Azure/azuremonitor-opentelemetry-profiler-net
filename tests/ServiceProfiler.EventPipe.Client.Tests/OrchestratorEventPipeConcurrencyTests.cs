// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;
using Microsoft.ServiceProfiler.Orchestration;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests;

public class OrchestratorEventPipeConcurrencyTests
{
    [Fact]
    public async Task StartProfilingAsync_WhenLeaseUnavailable_DoesNotStartProfiler()
    {
        Mock<IServiceProfilerProvider> provider = new();
        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IAsyncDisposable)null);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        bool result = await orchestrator.StartProfilingAsync(policy, CancellationToken.None);

        Assert.False(result);
        provider.Verify(
            p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartProfilingAsync_WhenLeaseGranted_StartsProfiler()
    {
        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));

        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        bool result = await orchestrator.StartProfilingAsync(policy, CancellationToken.None);

        Assert.True(result);
        provider.Verify(
            p => p.StartServiceProfilerAsync(policy, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopProfilingAsync_ReleasesLease()
    {
        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        provider.Setup(p => p.StopServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));

        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        Assert.True(await orchestrator.StartProfilingAsync(policy, CancellationToken.None));
        Assert.True(await orchestrator.StopProfilingAsync(policy, CancellationToken.None));

        leaseHandle.Verify(l => l.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartProfilingAsync_WhenProfilerFailsToStart_ReleasesLease()
    {
        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));

        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        bool result = await orchestrator.StartProfilingAsync(policy, CancellationToken.None);

        Assert.False(result);
        leaseHandle.Verify(l => l.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartProfilingAsync_WhenProviderThrowsWhileRunning_StopsAndReleasesLease()
    {
        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("threw after starting"));
        // The provider reports running (e.g. its semaphore is held) despite throwing.
        provider.SetupGet(p => p.IsProfilerRunning).Returns(true);
        provider.Setup(p => p.StopServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));

        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => orchestrator.StartProfilingAsync(policy, CancellationToken.None));

        // A failed start must never retain the lease: the profiler is best-effort stopped and the
        // lease released immediately, so it cannot be leaked regardless of provider state.
        provider.Verify(
            p => p.StopServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()),
            Times.Once);
        leaseHandle.Verify(l => l.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StopProfilingAsync_WhenStopFailsDuringCancellation_ReleasesLease()
    {
        using CancellationTokenSource cts = new();

        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // The stop is cancelled mid-flight (e.g. agent deactivation/shutdown) and throws while the
        // profiler still reports running. The token is cancelled as part of the call.
        provider.Setup(p => p.StopServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .Callback(() => cts.Cancel())
            .ThrowsAsync(new OperationCanceledException());
        provider.SetupGet(p => p.IsProfilerRunning).Returns(true);

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));

        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency);
        TestPolicy policy = new();

        Assert.True(await orchestrator.StartProfilingAsync(policy, CancellationToken.None));

        // The stop is cancelled and throws; because it was cancelled (terminal), the lease must be
        // released (best-effort stop first) instead of retained, so it cannot renew forever.
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.StopProfilingAsync(policy, cts.Token));

        leaseHandle.Verify(l => l.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task StartProfilingAsync_WhenGateHeldByAnotherStart_LogsDebugWithInFlightOperationWithoutWarning()
    {
        // Expected start-policy contention is Debug, but must still identify the in-flight start
        // even before _currentProfilingPolicy is assigned (issues #193 and #164).
        TaskCompletionSource startEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseStart = new(TaskCreationOptions.RunContinuationsAsynchronously);

        Mock<IServiceProfilerProvider> provider = new();
        provider.Setup(p => p.StartServiceProfilerAsync(It.IsAny<IProfilerSource>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                // Signal that the gate is now held, then block so it stays held.
                startEntered.TrySetResult();
                return releaseStart.Task;
            });

        Mock<IAsyncDisposable> leaseHandle = new();
        leaseHandle.Setup(l => l.DisposeAsync()).Returns(default(ValueTask));
        Mock<IProfilerConcurrencyControlClient> concurrency = new();
        concurrency.Setup(c => c.TryAcquireLeaseAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(leaseHandle.Object);

        CapturingLogger logger = new();
        TestOrchestrator orchestrator = CreateOrchestrator(provider, concurrency, logger);

        TestPolicy holder = new("HoldingPolicy");
        TestPolicy waiter = new("WaitingPolicy");

        Task<bool> holderTask = orchestrator.StartProfilingAsync(holder, CancellationToken.None);
        await startEntered.Task; // The gate is now held by the holder start.

        // This call cannot acquire the gate within its 500ms timeout.
        bool waiterResult = await orchestrator.StartProfilingAsync(waiter, CancellationToken.None);

        // Let the holder finish.
        releaseStart.SetResult(true);
        Assert.True(await holderTask);

        Assert.False(waiterResult);

        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
        string diagnostic = Assert.Single(logger.Entries.Where(
            e => e.Level == LogLevel.Debug && e.Message.Contains("policy-change gate"))).Message;
        Assert.Contains("500ms", diagnostic);
        Assert.Contains("WaitingPolicy", diagnostic);
        Assert.Contains("start by HoldingPolicy", diagnostic);
        Assert.DoesNotContain("(null)", diagnostic);
        provider.Verify(
            p => p.StartServiceProfilerAsync(holder, It.IsAny<CancellationToken>()),
            Times.Once);
        provider.Verify(
            p => p.StartServiceProfilerAsync(waiter, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnAgentStatusChanged_WhenActiveIsReasserted_DoesNotLogWarning()
    {
        // Regression test for issue #190: the agent status is re-asserted as Active on every periodic
        // heartbeat (every two hours). Reconciling to an already-active state is an expected no-op and
        // must not emit warning-level noise.
        using AgentStatusHarness harness = new();

        // The initial activation starts the schedules.
        await harness.NotifyActiveAsync("Initial activation");
        await harness.WaitForSchedulesStartedAsync();

        // The two-hour heartbeat re-asserts the unchanged Active status.
        await harness.NotifyActiveAsync("Refresh");

        Assert.DoesNotContain(harness.LogEntries, e => e.Level >= LogLevel.Warning);
        Assert.Contains(
            harness.LogEntries,
            e => e.Level == LogLevel.Debug && e.Message.Contains("The schedules are already running."));
    }

    [Fact]
    public async Task OnAgentStatusChanged_WhenActivatingWhileSchedulesAreStopping_LogsWarning()
    {
        // The counterpart to issue #190: an activation that arrives while the previous schedules are
        // still unwinding after a deactivation is genuinely dropped, so it must stay at warning level
        // rather than being demoted along with the routine heartbeat re-assertion.
        using AgentStatusHarness harness = new(unblockPolicyOnCancellation: false);

        await harness.NotifyActiveAsync("Initial activation");
        await harness.WaitForSchedulesStartedAsync();

        // Deactivation cancels the schedules, but the blocked policy has not finished unwinding yet.
        await harness.NotifyInactiveAsync("Deactivated");
        await harness.NotifyActiveAsync("Reactivated");

        Assert.Contains(
            harness.LogEntries,
            e => e.Level == LogLevel.Warning && e.Message.Contains("still stopping after a deactivation"));
    }

    private static TestOrchestrator CreateOrchestrator(
        Mock<IServiceProfilerProvider> provider,
        Mock<IProfilerConcurrencyControlClient> concurrency,
        ILogger<OrchestratorEventPipe> logger = null)
    {
        IOptions<UserConfigurationBase> options = Options.Create<UserConfigurationBase>(new TestUserConfiguration());
        return new TestOrchestrator(
            provider.Object,
            options,
            Mock.Of<IDelaySource>(),
            Mock.Of<IAgentStatusService>(),
            Mock.Of<IResourceUsageSource>(),
            concurrency.Object,
            logger ?? NullLogger<OrchestratorEventPipe>.Instance);
    }

    /// <summary>
    /// Drives <c>OrchestratorEventPipe.OnAgentStatusChanged</c> directly with a policy that keeps the
    /// orchestrator's schedules task running.
    /// <para>
    /// The <c>AgentStatus</c> enum lives in Microsoft.ServiceProfiler.Contract.Agent.Profiler, which does
    /// not grant InternalsVisibleTo to this assembly. Because that enum appears in the signature of
    /// <see cref="IAgentStatusService.StatusChanged"/>, a fake status service cannot even be declared
    /// here, so the status values and the handler are reached by reflection instead.
    /// </para>
    /// </summary>
    private sealed class AgentStatusHarness : IDisposable
    {
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

        private readonly MethodInfo _onAgentStatusChanged;
        private readonly object _active;
        private readonly object _inactive;
        private readonly BlockingPolicy _policy;
        private readonly CapturingLogger _logger = new();
        private readonly TestOrchestrator _orchestrator;

        /// <param name="unblockPolicyOnCancellation">
        /// When false, the policy keeps running after a deactivation cancels it, which models a schedules
        /// task that is still unwinding when the next activation arrives.
        /// </param>
        public AgentStatusHarness(bool unblockPolicyOnCancellation = true)
        {
            _policy = new BlockingPolicy(unblockPolicyOnCancellation);

            _onAgentStatusChanged = typeof(OrchestratorEventPipe).GetMethod(
                "OnAgentStatusChanged", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(_onAgentStatusChanged);

            Type agentStatusType = _onAgentStatusChanged.GetParameters()[0].ParameterType;
            _active = Enum.Parse(agentStatusType, "Active");
            _inactive = Enum.Parse(agentStatusType, "Inactive");

            _orchestrator = new TestOrchestrator(
                Mock.Of<IServiceProfilerProvider>(),
                Options.Create<UserConfigurationBase>(new TestUserConfiguration()),
                Mock.Of<IDelaySource>(),
                Mock.Of<IAgentStatusService>(),
                Mock.Of<IResourceUsageSource>(),
                Mock.Of<IProfilerConcurrencyControlClient>(),
                _logger,
                new[] { _policy });
            _policy.RegisterToOrchestrator(_orchestrator);
        }

        /// <summary>
        /// A snapshot of the entries logged so far, safe to enumerate while the schedules task runs.
        /// </summary>
        public IReadOnlyList<(LogLevel Level, string Message)> LogEntries => _logger.Snapshot();

        public Task NotifyActiveAsync(string reason) => Notify(_active, reason);

        public Task NotifyInactiveAsync(string reason) => Notify(_inactive, reason);

        public async Task WaitForSchedulesStartedAsync()
        {
            Task completed = await Task.WhenAny(_policy.Started.Task, Task.Delay(WaitTimeout));
            Assert.Same(_policy.Started.Task, completed);
        }

        public void Dispose()
        {
            // Order matters: cancel first so the policy loop observes cancellation, then release the
            // blocked iterator. Releasing without cancelling would turn StartPolicyAsync into a spin
            // loop, because its schedule would complete synchronously forever.
            try
            {
                _orchestrator.Dispose();
            }
            finally
            {
                _policy.Release();
            }
        }

        private Task Notify(object status, string reason)
            => (Task)_onAgentStatusChanged.Invoke(_orchestrator, new[] { status, reason });
    }

    /// <summary>
    /// A policy that blocks inside its schedule so the orchestrator's schedules task stays running.
    /// </summary>
    private sealed class BlockingPolicy : SchedulingPolicy
    {
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly bool _unblockOnCancellation;

        /// <param name="unblockOnCancellation">
        /// When false, the policy keeps blocking after its token is cancelled, which models a schedule
        /// that is still unwinding after a deactivation.
        /// </param>
        public BlockingPolicy(bool unblockOnCancellation = true)
            : base(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, Mock.Of<IDelaySource>(), Mock.Of<IExpirationPolicy>(), NullLogger<SchedulingPolicy>.Instance)
        {
            _unblockOnCancellation = unblockOnCancellation;
        }

        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override string Source => nameof(BlockingPolicy);

        public void Release() => _release.TrySetResult(true);

        public override async IAsyncEnumerable<(TimeSpan duration, ProfilerAction action)> GetScheduleAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Started.TrySetResult();

            // Park here so the orchestrator's schedules task stays incomplete for the duration of the
            // test. The release is always awaited, so the policy loop in SchedulingPolicy.StartPolicyAsync
            // keeps a suspension point and can never spin.
            TaskCompletionSource<bool> cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                if (_unblockOnCancellation)
                {
                    await Task.WhenAny(_release.Task, cancelled.Task).ConfigureAwait(false);
                }
                else
                {
                    await _release.Task.ConfigureAwait(false);
                }
            }

            yield break;
        }
    }

    private sealed class CapturingLogger : ILogger<OrchestratorEventPipe>
    {
        private readonly object _gate = new();
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        /// <summary>
        /// Returns a copy of the entries logged so far. Required when the orchestrator may still be
        /// logging from its schedules task while a test inspects the output.
        /// </summary>
        public IReadOnlyList<(LogLevel Level, string Message)> Snapshot()
        {
            lock (_gate)
            {
                return Entries.ToList();
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (_gate)
            {
                Entries.Add((logLevel, formatter(state, exception)));
            }
        }
    }

    private sealed class TestUserConfiguration : UserConfigurationBase
    {
    }

    private sealed class TestOrchestrator : OrchestratorEventPipe
    {
        public TestOrchestrator(
            IServiceProfilerProvider profilerProvider,
            IOptions<UserConfigurationBase> config,
            IDelaySource delaySource,
            IAgentStatusService agentStatusService,
            IResourceUsageSource resourceUsageSource,
            IProfilerConcurrencyControlClient concurrencyControlClient,
            ILogger<OrchestratorEventPipe> logger,
            IEnumerable<SchedulingPolicy> policies = null)
            : base(
                profilerProvider,
                config,
                policies ?? Array.Empty<SchedulingPolicy>(),
                delaySource,
                agentStatusService,
                resourceUsageSource,
                logger,
                concurrencyControlClient)
        {
        }
    }

    private sealed class TestPolicy : SchedulingPolicy
    {
        private readonly string _source;

        public TestPolicy(string source = nameof(TestPolicy))
            : base(TimeSpan.Zero, TimeSpan.Zero, TimeSpan.Zero, Mock.Of<IDelaySource>(), Mock.Of<IExpirationPolicy>(), NullLogger<SchedulingPolicy>.Instance)
        {
            _source = source;
        }

        public override string Source => _source;

        public override IAsyncEnumerable<(TimeSpan duration, ProfilerAction action)> GetScheduleAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }
}