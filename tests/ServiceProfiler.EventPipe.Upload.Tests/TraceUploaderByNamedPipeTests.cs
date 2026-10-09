// -----------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Uploader;
using Microsoft.ApplicationInsights.Profiler.Uploader.Stubs;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.ServiceProfiler.Agent;
using Microsoft.ServiceProfiler.Contract.Agent;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Upload.Tests;

/// <summary>
/// Regression tests for issue #192: the additional-data read used a hard-coded 500ms timeout while
/// the profiler was allowed minutes to send it, so a loaded machine could lose an otherwise viable
/// trace.
/// </summary>
public class TraceUploaderByNamedPipeTests
{
    private static readonly Guid TestAppId = Guid.Parse("8b0b8b0b-0b0b-4b0b-8b0b-0b0b0b0b0b0b");

    [Fact]
    public async Task UploadingAsync_ReadsAdditionalDataWithTheSameBudgetTheSenderUses()
    {
        // The hard-coded 500ms had to cover the profiler waking from its own read, deriving the
        // artifact id, building the payload over every sample and serializing it. Both ends now use
        // ExtendedMessageTimeout, so the reader cannot give up while the sender is still well
        // within its own budget.
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        TimeSpan? observedTimeout = null;
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<TimeSpan, CancellationToken>((timeout, _) => observedTimeout = timeout)
            .ReturnsAsync(CreateAdditionalData());

        TraceUploaderByNamedPipe target = CreateTarget(pipe);

        await target.UploadingAsync(CreateUploadContext(), CancellationToken.None);

        Assert.Equal(NamedPipeOptions.ExtendedMessageTimeout, observedTimeout);
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataArrives_ReturnsItOnTheContext()
    {
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        IPCAdditionalData additionalData = CreateAdditionalData();
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(additionalData);

        TraceUploaderByNamedPipe target = CreateTarget(pipe);

        UploadContextExtension? result = await target.UploadingAsync(CreateUploadContext(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(additionalData, result!.AdditionalData);
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataTimesOut_FailsWithAnActionableMessage()
    {
        // The payload is not optional enrichment: it carries the connection string used to emit the
        // custom events and the index event through which the trace is discovered. Continuing
        // without it would upload a trace that can never be surfaced, so failing is correct - but
        // the bare "Can't finish reading message within given timeout" says nothing about which
        // message was lost or why that ends the upload.
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Can't finish reading message within given timeout: 500ms"));

        TraceUploaderByNamedPipe target = CreateTarget(pipe);

        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(
            () => target.UploadingAsync(CreateUploadContext(), CancellationToken.None));

        Assert.Contains("additional data", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("abandoned", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataTimesOut_FailsBeforeAnythingIsUploaded()
    {
        // The read happens before the trace is zipped and uploaded, so an abandoned upload must not
        // leave an orphaned blob behind.
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("timed out"));

        Mock<IZipUtility> zip = new();
        Mock<IBlobClientFactory> blobClientFactory = new();
        TraceUploaderByNamedPipe target = CreateTarget(pipe, zip, blobClientFactory);

        await Assert.ThrowsAsync<TimeoutException>(
            () => target.UploadAsync(CancellationToken.None));

        zip.Verify(z => z.ZipFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        blobClientFactory.Verify(f => f.CreateBlobClient(It.IsAny<Uri>()), Times.Never);
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataIsNull_FailsBeforeAnythingIsUploaded()
    {
        // A payload that arrives but deserializes to null is the same outcome as one that never
        // arrives. Letting it through would upload and commit a blob that is then orphaned when the
        // custom events cannot be sent.
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IPCAdditionalData?)null);

        Mock<IZipUtility> zip = new();
        TraceUploaderByNamedPipe target = CreateTarget(pipe, zip);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => target.UploadAsync(CancellationToken.None));

        zip.Verify(z => z.ZipFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public void ExtendedMessageTimeout_StaysGenerousEnoughForASlowProfiler()
    {
        // Both ends of the additional-data exchange are pinned to this one constant, so shrinking
        // it would recreate #192 on both sides at once while the per-call-site assertions above
        // still passed. Guard the property that actually matters: it must stay well clear of the
        // ordinary per-message budget, because it has to cover the profiler building the payload
        // over every sample on a loaded machine - not just transit.
        Assert.True(
            NamedPipeOptions.ExtendedMessageTimeout >= TimeSpan.FromMinutes(5),
            $"ExtendedMessageTimeout is {NamedPipeOptions.ExtendedMessageTimeout}, too short to cover a slow profiler.");
        Assert.True(
            NamedPipeOptions.ExtendedMessageTimeout > new NamedPipeOptions().DefaultMessageTimeout,
            "ExtendedMessageTimeout must exceed the ordinary per-message default, otherwise it serves no purpose.");
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataHasNoConnectionString_FailsBeforeAnythingIsUploaded()
    {
        // Rejecting only null would let an incomplete payload through to the blob upload, where
        // BuildTelemetryConfiguration then throws after the artifact is already committed.
        IPCAdditionalData incomplete = CreateAdditionalData() with { ConnectionString = null };

        await AssertRejectedBeforeUploadAsync(incomplete);
    }

    [Fact]
    public async Task UploadingAsync_WhenAdditionalDataHasNoIndex_FailsBeforeAnythingIsUploaded()
    {
        // Without the index event the trace uploads but can never be surfaced - the orphan this
        // guard exists to prevent.
        IPCAdditionalData incomplete = CreateAdditionalData() with { ServiceProfilerIndex = null! };

        await AssertRejectedBeforeUploadAsync(incomplete);
    }

    private static async Task AssertRejectedBeforeUploadAsync(IPCAdditionalData? additionalData)
    {
        Mock<INamedPipeServerService> pipe = CreateConnectedPipe();
        pipe.Setup(p => p.ReadAsync<IPCAdditionalData>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(additionalData);

        Mock<IZipUtility> zip = new();
        Mock<IBlobClientFactory> blobClientFactory = new();
        TraceUploaderByNamedPipe target = CreateTarget(pipe, zip, blobClientFactory);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => target.UploadAsync(CancellationToken.None));

        zip.Verify(z => z.ZipFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        blobClientFactory.Verify(f => f.CreateBlobClient(It.IsAny<Uri>()), Times.Never);
    }

    private static Mock<INamedPipeServerService> CreateConnectedPipe()
    {
        Mock<INamedPipeServerService> pipe = new();
        pipe.Setup(p => p.WaitForConnectionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        pipe.Setup(p => p.SendAsync(It.IsAny<IEnumerable<SampleActivity>>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        pipe.Setup(p => p.SendAsync(It.IsAny<Guid>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        pipe.Setup(p => p.ReadAsync<IEnumerable<SampleActivity>>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SampleActivity()]);
        pipe.Setup(p => p.ReadAsync<AccessTokenContract>(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AccessTokenContract?)null);
        return pipe;
    }

    private static IPCAdditionalData CreateAdditionalData() => new()
    {
        ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000",
        ArtifactId = Guid.NewGuid(),
        ServiceProfilerIndex = new ServiceProfilerIndex
        {
            Timestamp = DateTime.UtcNow,
            StampId = "test-stamp",
            DataCube = "00000000-0000-0000-0000-000000000000",
            EtlFileSessionId = DateTimeOffset.UtcNow.ToString("o"),
            ArtifactId = Guid.NewGuid(),
            ArtifactKind = "profile",
            ProgrammingLanguage = "DotNet",
            OperatingSystem = "Windows",
            CloudRoleName = "test-role",
        },
        ServiceProfilerSamples = [],
        AgentString = "test-agent",
    };

    private static UploadContext CreateUploadContext() => new()
    {
        AIInstrumentationKey = Guid.NewGuid(),
        HostUrl = new Uri("https://localhost"),
        StampId = "test-stamp",
        SessionId = DateTimeOffset.UtcNow,
        TraceFilePath = "trace.nettrace",
        MetadataFilePath = "trace.metadata",
        PipeName = "test-pipe",
        UploadMode = UploadMode.Always,
    };

    private static TraceUploaderByNamedPipe CreateTarget(
        Mock<INamedPipeServerService> pipe,
        Mock<IZipUtility>? zipUtility = null,
        Mock<IBlobClientFactory>? blobClientFactory = null)
    {
        Mock<INamedPipeServerFactory> pipeFactory = new();
        pipeFactory.Setup(f => f.CreateNamedPipeService()).Returns(pipe.Object);

        Mock<IAppProfileClient> appProfileClient = new();
        appProfileClient
            .Setup(c => c.GetAppProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppProfileResponse { AppId = TestAppId });
        Mock<IAppProfileClientFactory> appProfileClientFactory = new();
        appProfileClientFactory.Setup(f => f.Create(It.IsAny<UploadContextExtension>())).Returns(appProfileClient.Object);

        Mock<ITraceValidatorFactory> traceValidatorFactory = new();
        traceValidatorFactory.Setup(f => f.Create(It.IsAny<string>())).Returns(new AlwaysPassValidator());

        Mock<IUploadContextValidator> uploadContextValidator = new();
        uploadContextValidator.Setup(v => v.Validate(It.IsAny<UploadContext>())).Returns(string.Empty);

        return new TraceUploaderByNamedPipe(
            (zipUtility ?? new Mock<IZipUtility>()).Object,
            (blobClientFactory ?? new Mock<IBlobClientFactory>()).Object,
            Mock.Of<IProfilerClientFactory>(),
            Mock.Of<IAppInsightsLogger>(),
            Mock.Of<IOSPlatformProvider>(),
            traceValidatorFactory.Object,
            Mock.Of<ISampleActivitySerializer>(),
            CreateUploadContext(),
            uploadContextValidator.Object,
            pipeFactory.Object,
            appProfileClientFactory.Object,
            Mock.Of<ICustomEventsSender>(),
            NullLogger<TraceUploaderByNamedPipe>.Instance);
    }
}
