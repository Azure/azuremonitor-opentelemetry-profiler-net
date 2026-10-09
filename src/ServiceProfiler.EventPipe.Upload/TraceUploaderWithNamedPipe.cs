using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Auth;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.Logging;
using ServiceProfiler.EventPipe.Upload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Uploader;

internal class TraceUploaderByNamedPipe : TraceUploader
{
    private readonly INamedPipeServerFactory _namedPipeServerFactory;

    public TraceUploaderByNamedPipe(
        IZipUtility zipUtility,
        IBlobClientFactory blobClientFactory,
        IProfilerClientFactory profilerClientFactory,
        IAppInsightsLogger telemetryLogger,
        IOSPlatformProvider oSPlatformProvider,
        ITraceValidatorFactory traceValidatorFactory,
        ISampleActivitySerializer sampleActivitySerializer,
        UploadContext uploadContext,
        IUploadContextValidator uploadContextValidator,
        INamedPipeServerFactory namedPipeServerFactory,
        IAppProfileClientFactory appProfileClientFactory,
        ICustomEventsSender customEventsSender,
        ILogger<TraceUploaderByNamedPipe> logger)
        : base(zipUtility,
            blobClientFactory,
            profilerClientFactory,
            telemetryLogger,
            oSPlatformProvider,
            traceValidatorFactory,
            sampleActivitySerializer,
            uploadContext,
            uploadContextValidator,
            appProfileClientFactory,
            customEventsSender,
            logger)
    {
        _namedPipeServerFactory = namedPipeServerFactory ?? throw new ArgumentNullException(nameof(namedPipeServerFactory));
    }

    protected internal override async Task<UploadContextExtension?> UploadingAsync(UploadContext uploadContext, CancellationToken cancellationToken = default)
    {
        // Making sure upload context is valid;
        string details = UploadContextValidator.Validate(UploadContext);
        UploadContextExtension uploadContextExtension = new(uploadContext);

        if (!string.IsNullOrEmpty(details))
        {
            Logger.LogError("UploadContext validation failed. Details: {errorDetails}", details);
            return null;
        }

        INamedPipeServerService namedPipeServer = _namedPipeServerFactory.CreateNamedPipeService();

        try
        {
            // Start namedpipe server for connecting in:
            Logger.LogTrace("Starting namedpipe by name: {pipeName}", UploadContext.PipeName);
            await namedPipeServer.WaitForConnectionAsync(UploadContext.PipeName!, cancellationToken).ConfigureAwait(false);
            Logger.LogTrace("Connection established.");

            IEnumerable<SampleActivity> samples = Enumerable.Empty<SampleActivity>();
            // Making sure there are samples for uploading.
            samples = await namedPipeServer.ReadAsync<IEnumerable<SampleActivity>>(cancellationToken: cancellationToken).ConfigureAwait(false) ?? Enumerable.Empty<SampleActivity>();
            Logger.LogDebug("{totalSampleCount} samples in total for uploading.", samples.Count());

            samples = GetValidSamples(UploadContext.TraceFilePath, samples);
            // Contract with Profiler Client: Serialize back so that the profiler knows to drop application insights custom events accordingly.
            Logger.LogTrace("Sending valid activities back...");
            await namedPipeServer.SendAsync(samples, cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.LogTrace("Sent valid activities back.");

            Logger.LogTrace("Receiving access token");
            uploadContextExtension.TokenCredential = null;
            AccessTokenContract? accessTokenContract = await namedPipeServer.ReadAsync<AccessTokenContract>(cancellationToken: cancellationToken).ConfigureAwait(false);
            // Only when access token has a non-default expiry:
            Logger.LogTrace("Got Access Token: {token} ..., Expires On: {expiresOn}", accessTokenContract?.Token?.Substring(0, 10), accessTokenContract?.ExpiresOn.ToLocalTime());
            if (accessTokenContract is not null)
            {
                Logger.LogTrace("Setup access token for AAD auth.");
                uploadContextExtension.TokenCredential = new StaticAccessTokenCredential(accessTokenContract.ToAccessToken());
            }

            uploadContextExtension.VerifiedAppId = (await AppProfileClientFactory.Create(uploadContextExtension).GetAppProfileAsync(uploadContext.AIInstrumentationKey.ToString("D"), cancellationToken).ConfigureAwait(false))?.AppId ?? Guid.Empty;
            Logger.LogTrace("Sending verified appId back ...");
            await namedPipeServer.SendAsync<Guid>(uploadContextExtension.VerifiedAppId, cancellationToken: cancellationToken).ConfigureAwait(false);
            Logger.LogTrace("Sent verified appId.");

            Logger.LogTrace("Receiving additional data");
            uploadContextExtension.AdditionalData = await ReadAdditionalDataAsync(namedPipeServer, cancellationToken).ConfigureAwait(false);

            return uploadContextExtension.VerifiedAppId != Guid.Empty && ShouldUploadTrace(UploadContext.UploadMode, samples.Count()) ?
                uploadContextExtension
                : null;
        }
        finally
        {
            (namedPipeServer as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// Reads the <see cref="IPCAdditionalData"/> payload sent by the profiler.
    /// </summary>
    /// <remarks>
    /// This payload is not optional enrichment: it carries the connection string used to emit the
    /// custom events, and the ServiceProfilerIndex event through which the trace is discovered.
    /// Without it an uploaded trace is orphaned - storage is consumed and the upload reports
    /// success, but the trace can never be surfaced. Failing here is therefore the correct outcome,
    /// and it happens before the trace is zipped and uploaded, so nothing is wasted.
    /// <para>
    /// The read uses <see cref="NamedPipeOptions.ExtendedMessageTimeout"/>, the same budget the
    /// profiler uses to send it. It previously used a hard-coded 500ms, which had to cover the
    /// profiler waking from its own read, deriving the artifact id, building this payload over
    /// every sample and serializing it - so a loaded machine could exceed it while the profiler was
    /// working normally, and the trace was discarded.
    /// </para>
    /// </remarks>
    private async Task<IPCAdditionalData> ReadAdditionalDataAsync(INamedPipeServerService namedPipeServer, CancellationToken cancellationToken)
    {
        IPCAdditionalData? additionalData;
        try
        {
            additionalData = await namedPipeServer.ReadAsync<IPCAdditionalData>(NamedPipeOptions.ExtendedMessageTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (TimeoutException ex)
        {
            // Rethrow with the consequence spelled out. The bare "Can't finish reading message
            // within given timeout" gives no indication of which message was lost or why that ends
            // the upload.
            throw new TimeoutException(
                "Timed out waiting for the profiler to send the additional data (connection string, " +
                "index and samples). The trace cannot be indexed without it, so the upload is abandoned. " +
                "This usually means the profiler process was starved or stopped before it could send.",
                ex);
        }

        // A payload that arrives but is missing what makes the trace discoverable is the same
        // outcome as one that never arrives: the blob would upload and commit, and then either the
        // index event is skipped or the telemetry configuration throws - after the artifact is
        // already committed. Check here, before anything is zipped or uploaded, so the failure
        // stays cheap and leaves nothing behind.
        if (additionalData is null)
        {
            throw new InvalidOperationException(
                "The profiler sent no additional data (connection string, index and samples). " +
                "The trace cannot be indexed without it, so the upload is abandoned.");
        }

        if (string.IsNullOrEmpty(additionalData.ConnectionString))
        {
            throw new InvalidOperationException(
                "The additional data from the profiler carries no connection string, so the custom " +
                "events that make the trace discoverable cannot be sent. The upload is abandoned.");
        }

        if (additionalData.ServiceProfilerIndex is null)
        {
            throw new InvalidOperationException(
                "The additional data from the profiler carries no index, so the trace could be " +
                "uploaded but never surfaced. The upload is abandoned.");
        }

        Logger.LogTrace("Additional data received");
        return additionalData;
    }
}
