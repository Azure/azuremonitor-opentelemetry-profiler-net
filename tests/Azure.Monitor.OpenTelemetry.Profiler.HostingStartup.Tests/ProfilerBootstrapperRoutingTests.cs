// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.Monitor.OpenTelemetry.Profiler.HostingStartup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Azure.Monitor.OpenTelemetry.Profiler.HostingStartupTests;

public class ProfilerBootstrapperRoutingTests
{
    private static (Mock<IWebHostBuilder> Builder, List<Action<IServiceCollection>> Captured) CreateBuilder()
    {
        List<Action<IServiceCollection>> captured = new();
        Mock<IWebHostBuilder> builder = new();
        builder
            .Setup(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()))
            .Callback<Action<IServiceCollection>>(captured.Add)
            .Returns(() => builder.Object);
        return (builder, captured);
    }

    private static ProfilerBootstrapper CreateBootstrapper(
        TelemetryStack detected,
        IProfilerActivatorInvoker invoker)
    {
        Mock<ITelemetryStackDetector> detector = new();
        detector.Setup(d => d.Detect()).Returns(detected);
        return new ProfilerBootstrapper(detector.Object, invoker);
    }

    [Fact]
    internal void Apply_WhenOpenTelemetry_DeferredCallbackInvokesOpenTelemetryActivator()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.OpenTelemetry);

        // Exactly one deferred ConfigureServices registration; running it invokes the OpenTelemetry activator.
        Action<IServiceCollection> register = Assert.Single(captured);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);

        ServiceCollection services = new();
        register(services);
        invoker.Verify(i => i.Invoke(TelemetryStack.OpenTelemetry, services), Times.Once);
    }

    [Fact]
    internal void Apply_WhenLegacyApplicationInsights_DeferredCallbackInvokesClassicActivator()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.LegacyApplicationInsights);

        Action<IServiceCollection> register = Assert.Single(captured);
        ServiceCollection services = new();
        register(services);
        invoker.Verify(i => i.Invoke(TelemetryStack.LegacyApplicationInsights, services), Times.Once);
    }

    [Fact]
    internal void Apply_WhenNone_DoesNotRegisterAnything()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.None);

        Assert.Empty(captured);
        builder.Verify(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Never);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Configure_DispatchesDetectedStack()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.OpenTelemetry, invoker.Object);

        bootstrapper.Configure(builder.Object);

        Assert.Single(captured);
    }

    [Fact]
    internal void Configure_PrefersRecordedDecisionOverDetector()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        // Detector would say "None", but the StartupHook recorded "OpenTelemetry" - the recording must win.
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        AppContext.SetData(DetectedStackAppContextData.Key, TelemetryStack.OpenTelemetry.ToString());
        try
        {
            bootstrapper.Configure(builder.Object);

            Action<IServiceCollection> register = Assert.Single(captured);
            ServiceCollection services = new();
            register(services);
            invoker.Verify(i => i.Invoke(TelemetryStack.OpenTelemetry, services), Times.Once);
        }
        finally
        {
            AppContext.SetData(DetectedStackAppContextData.Key, null);
        }
    }

    [Fact]
    internal void Configure_WhenNoStackDetected_NonDotNetApp_DoesNotInvokeActivator()
    {
        // Models a non-.NET App Service app: the StartupHook records nothing (a non-.NET worker never loads
        // it) and detection returns None, so the router must not register anything or attempt to load/invoke
        // any activator - the extension is a safe no-op.
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Configure(builder.Object);

        Assert.Empty(captured);
        builder.Verify(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Never);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Apply_WhenAlreadyInstrumented_BacksOffAndRegistersNothing()
    {
        // The app already references a profiler NuGet and activates it in code, so codeless enablement must
        // register nothing and never invoke an activator (no double activation).
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.AlreadyInstrumented);

        Assert.Empty(captured);
        builder.Verify(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Never);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Apply_WhenAgentInstrumentedNoSdk_BacksOffAndRegistersNothing()
    {
        // App Service AI agent instruments at runtime but no SDK is referenced: we only log a recommendation,
        // never activate.
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.AgentInstrumentedNoSdk);

        Assert.Empty(captured);
        builder.Verify(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Never);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Apply_WhenAzureFunctionsPlatformHost_BacksOffAndRegistersNothing()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.AzureFunctionsPlatformHost);

        Assert.Empty(captured);
        builder.Verify(b => b.ConfigureServices(It.IsAny<Action<IServiceCollection>>()), Times.Never);
        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Apply_WhenDependencyFloorViolation_DoesNotInvokeActivator()
    {
        // A below-floor shared dependency loaded by the app: the deferred callback must back off (log) and
        // NOT invoke the activator, rather than letting activation throw-and-catch.
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        Mock<ITelemetryStackDetector> detector = new();
        Mock<IDependencyFloorChecker> checker = new();
        checker
            .Setup(c => c.CheckLoadedAgainstPayloadFloors(It.IsAny<TelemetryStack>()))
            .Returns(new[] { new DependencyFloorViolation("OpenTelemetry", new Version(1, 7, 0, 0), new Version(1, 8, 1, 0)) });
        ProfilerBootstrapper bootstrapper = new(detector.Object, invoker.Object, checker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.OpenTelemetry);
        Action<IServiceCollection> register = Assert.Single(captured);
        register(new ServiceCollection());

        invoker.Verify(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()), Times.Never);
    }

    [Fact]
    internal void Apply_WhenNoDependencyFloorViolation_InvokesActivator()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        Mock<ITelemetryStackDetector> detector = new();
        Mock<IDependencyFloorChecker> checker = new();
        checker
            .Setup(c => c.CheckLoadedAgainstPayloadFloors(It.IsAny<TelemetryStack>()))
            .Returns(Array.Empty<DependencyFloorViolation>());
        ProfilerBootstrapper bootstrapper = new(detector.Object, invoker.Object, checker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.OpenTelemetry);
        Action<IServiceCollection> register = Assert.Single(captured);
        ServiceCollection services = new();
        register(services);

        invoker.Verify(i => i.Invoke(TelemetryStack.OpenTelemetry, services), Times.Once);
    }

    [Fact]
    internal void Configure_WhenDetectorThrows_DoesNotThrow()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> _) = CreateBuilder();
        Mock<ITelemetryStackDetector> detector = new();
        detector.Setup(d => d.Detect()).Throws(new InvalidOperationException("boom"));
        ProfilerBootstrapper bootstrapper = new(detector.Object, Mock.Of<IProfilerActivatorInvoker>());

        // Bootstrap failures must never break host startup.
        bootstrapper.Configure(builder.Object);
    }

    [Fact]
    internal void Apply_WhenActivatorInvokeThrows_DeferredCallbackDoesNotPropagate()
    {
        (Mock<IWebHostBuilder> builder, List<Action<IServiceCollection>> captured) = CreateBuilder();
        Mock<IProfilerActivatorInvoker> invoker = new();
        // Simulates an incompatible-dependency failure (e.g. a MissingMethodException/FileNotFoundException
        // from a version the activator was compiled against but the app does not provide) surfacing while
        // loading/invoking the activator. The deferred ConfigureServices callback - which runs inside the
        // app's host build - MUST swallow it, otherwise the host crashes.
        invoker
            .Setup(i => i.Invoke(It.IsAny<TelemetryStack>(), It.IsAny<IServiceCollection>()))
            .Throws(new MissingMethodException("simulated version skew"));
        ProfilerBootstrapper bootstrapper = CreateBootstrapper(TelemetryStack.None, invoker.Object);

        bootstrapper.Apply(builder.Object, TelemetryStack.OpenTelemetry);
        Action<IServiceCollection> register = Assert.Single(captured);

        // Must not throw.
        register(new ServiceCollection());
    }
}
