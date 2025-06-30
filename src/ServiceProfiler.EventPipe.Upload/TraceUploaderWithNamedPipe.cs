using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
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
        IProfilerFrontendClientBuilder stampFrontendClientBuilder,
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
            stampFrontendClientBuilder,
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
            uploadContextExtension.AdditionalData = await namedPipeServer.ReadAsync<IPCAdditionalData>(timeout: TimeSpan.FromSeconds(0.5), cancellationToken).ConfigureAwait(false);
            Logger.LogTrace("Additional data received");

            return uploadContextExtension.VerifiedAppId != Guid.Empty && ShouldUploadTrace(UploadContext.UploadMode, samples.Count()) ?
                uploadContextExtension
                : null;
        }
        finally
        {
            (namedPipeServer as IDisposable)?.Dispose();
        }
    }
}
