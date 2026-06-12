//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure;
using Azure.Core;
using Azure.Monitor.Diagnostics.Models;
using Azure.Monitor.Diagnostics.Profiler;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Contract;
using Microsoft.ServiceProfiler.Contract.Agent;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.EventPipe.Upload;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.ApplicationInsights.Profiler.Uploader;

internal class TraceUploader : ITraceUploader
{
    private readonly IZipUtility _zipUtility;
    private readonly IAppInsightsLogger _telemetryLogger;
    private readonly IOSPlatformProvider _osPlatformProvider;
    private readonly ITraceValidatorFactory _traceValidatorFactory;
    private readonly ISampleActivitySerializer _sampleActivitySerializer;
    private readonly ICustomEventsSender _customEventsSender;
    private readonly IBlobClientFactory _blobClientFactory;
    private readonly IProfilerClientFactory _profilerClientFactory;

    protected IAppProfileClientFactory AppProfileClientFactory { get; }
    protected UploadContext UploadContext { get; }
    protected IUploadContextValidator UploadContextValidator { get; }
    protected ILogger Logger { get; }

    public TraceUploader(
        IZipUtility zipUtility,
        IBlobClientFactory blobClientFactory,
        IProfilerClientFactory profilerClientFactory,
        IAppInsightsLogger telemetryLogger,
        IOSPlatformProvider oSPlatformProvider,
        ITraceValidatorFactory traceValidatorFactory,
        ISampleActivitySerializer sampleActivitySerializer,
        UploadContext uploadContext,
        IUploadContextValidator uploadContextValidator,
        IAppProfileClientFactory appProfileClientFactory,
        ICustomEventsSender customEventsSender,
        ILogger<TraceUploader> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _zipUtility = zipUtility ?? throw new ArgumentNullException(nameof(zipUtility));
        _profilerClientFactory = profilerClientFactory ?? throw new ArgumentNullException(nameof(profilerClientFactory));
        _telemetryLogger = telemetryLogger ?? throw new ArgumentNullException(nameof(telemetryLogger));
        _osPlatformProvider = oSPlatformProvider ?? throw new ArgumentNullException(nameof(oSPlatformProvider));
        _traceValidatorFactory = traceValidatorFactory ?? throw new ArgumentNullException(nameof(traceValidatorFactory));
        _sampleActivitySerializer = sampleActivitySerializer ?? throw new ArgumentNullException(nameof(sampleActivitySerializer));
        UploadContext = uploadContext ?? throw new ArgumentNullException(nameof(uploadContext));
        UploadContextValidator = uploadContextValidator ?? throw new ArgumentNullException(nameof(uploadContextValidator));
        AppProfileClientFactory = appProfileClientFactory ?? throw new ArgumentNullException(nameof(appProfileClientFactory));
        _customEventsSender = customEventsSender ?? throw new ArgumentNullException(nameof(customEventsSender));
        _blobClientFactory = blobClientFactory ?? throw new ArgumentNullException(nameof(blobClientFactory));
    }

    /// <summary>
    /// Upload the trace.
    /// </summary>
    /// <param name="context">Payloads for the uploading parameters.</param>
    /// <param name="cancellationToken">The token to cancel the upload process.</param>
    /// <returns>The task of uploading the trace.</returns>
    public async Task UploadAsync(CancellationToken cancellationToken)
    {
        Logger.LogTrace("Start {methodName}", nameof(UploadAsync));
        UploadContext context = UploadContext;

        // Check samples;
        UploadContextExtension? extendedUploadContext = await UploadingAsync(context, cancellationToken).ConfigureAwait(false);
        if (extendedUploadContext == null)
        {
            Logger.LogTrace("Pre-check for uploading failed.");
            return;
        }

        // Extract
        string zippedFilePath = _zipUtility.ZipFile(context.TraceFilePath, additionalFiles: [context.MetadataFilePath]);

        // Upload
        await UploadAsync(extendedUploadContext, zippedFilePath, cancellationToken).ConfigureAwait(false);

        // Sending custom events
        _customEventsSender.Send(extendedUploadContext);
    }

    private async Task UploadAsync(UploadContextExtension extendedContext, string zippedFilePath, CancellationToken cancellationToken)
    {
        UploadContext context = extendedContext.UploadContext;
        try
        {
            IProfilerClient profilerClient = _profilerClientFactory.Create(extendedContext);

            // Prefer the artifact ID from the agent (via IPC) to ensure consistency with custom events.
            // Fall back to deriving it locally for the non-named-pipe path.
            Guid artifactId = extendedContext.AdditionalData?.ArtifactId
                ?? ArtifactIdDerivation.DeriveArtifactId(context.SessionId, EnvironmentUtilities.MachineName);
            Logger.LogDebug("Uploading artifact {artifactId}", artifactId);

            Uri blobUri = await profilerClient.GetProfilerArtifactUploadTokenAsync(artifactId, cancellationToken).ConfigureAwait(false);
            Logger.LogDebug("Got upload token for artifact {artifactId}", artifactId);

            BlobClient blob = _blobClientFactory.CreateBlobClient(blobUri);
            await blob.UploadAsync(zippedFilePath, overwrite: true, cancellationToken).ConfigureAwait(false);

            // Set metadata on the blob itself so that downstream ingestion can read it.
            Dictionary<string, string> metadata = CreateMetadata(extendedContext, artifactId);
            Azure.Response<BlobInfo> metadataResponse = await blob.SetMetadataAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Use the ETag from the metadata response (which is the latest after metadata was set).
            ETag etag = metadataResponse.Value.ETag;

            AcceptedArtifact acceptedArtifact = await profilerClient.CommitProfilerArtifactAsync(artifactId, etag, cancellationToken).ConfigureAwait(false);

            // The server returns the stamp ID in the commit response — propagate it
            // so that custom events can reference the correct artifact location.
            context.StampId = acceptedArtifact.StampId;

            Logger.LogDebug("Blob upload committed for artifact {artifactId} on stamp {stampId}.", artifactId, acceptedArtifact.StampId);
            Logger.LogInformation(TelemetryConstants.TraceUploaded);

            _telemetryLogger.Flush();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to upload trace artifact.");
            throw;
        }
        finally
        {
            if (!string.IsNullOrEmpty(zippedFilePath))
            {
                if (!context.PreserveTraceFile)
                {
                    try { File.Delete(zippedFilePath); }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch
#pragma warning restore CA1031 // Do not catch general exception types
                    { }
                }
                else
                {
                    Logger.LogInformation("Trace file will be preserved at: {zippedFilePath}", zippedFilePath);
                }
            }
        }
    }


    private Dictionary<string, string> CreateMetadata(UploadContextExtension extendedContext, Guid artifactId)
    {
        UploadContext context = extendedContext.UploadContext;
        Dictionary<string, string> metadata = new()
        {
            [BlobMetadataConstants.DataCubeMetaName] = BlobMetadata.GetDataCubeNameString(extendedContext.VerifiedAppId),
            [BlobMetadataConstants.ArtifactId] = StoragePathContract.GetArtifactIdString(artifactId),
            // Notice, the machine name on the metadata needs to include the iis name suffix when running in antares.
            // Otherwise, the blob won't be located and approved by the frontend.
            [BlobMetadataConstants.MachineNameMetaName] = EnvironmentUtilities.MachineName,
            [BlobMetadataConstants.StartTimeMetaName] = TimestampContract.TimestampToString(context.SessionId),
            [BlobMetadataConstants.TriggerTime] = TimestampContract.TimestampToString(context.SessionId),
            [BlobMetadataConstants.ProgrammingLanguageMetaName] = ProgramLanguages.CSharp,
            [BlobMetadataConstants.OSPlatformMetaName] = _osPlatformProvider.GetOSPlatformDescription(),
            [BlobMetadataConstants.TraceFileFormatMetaName] = context.TraceFileFormat,
        };

        if (TryGetCloudRoleName(context, out string? cloudRoleName))
        {
            metadata.Add(BlobMetadataConstants.RoleName, cloudRoleName!);
        }

        if (!string.IsNullOrEmpty(context.TriggerType))
        {
            metadata.Add(BlobMetadataConstants.TriggerType, context.TriggerType);
        }

        return metadata;
    }

    internal bool TryGetCloudRoleName(UploadContext context, out string? cloudRoleName, int maxLength = 64)
    {
        Debug.Assert(maxLength >= 0, "Why set maxLength <= 0?");

        cloudRoleName = null;
        if (string.IsNullOrEmpty(context.RoleName))
        {
            return false;
        }

        cloudRoleName = context.RoleName;

        // Arbitrary length limit for roleName to avoid exceed blob length cap
        if (cloudRoleName.Length > maxLength)
        {
            Logger.LogWarning("RoleName exceed length limit of {maxLength} characters. Consider to have a shorter roleName.", maxLength);
            cloudRoleName = cloudRoleName.Substring(startIndex: 0, length: maxLength);
        }

        return true;
    }

    /// <summary>
    /// Process before upload.
    /// </summary>
    internal protected virtual async Task<UploadContextExtension?> UploadingAsync(UploadContext uploadContext, CancellationToken cancellation = default)
    {
        // Making sure upload context is valid;
        string details = UploadContextValidator.Validate(UploadContext);
        if (!string.IsNullOrEmpty(details))
        {
            Logger.LogError("UploadContext validation failed. Details: {errorDetails}", details);
            return null;
        }

        // Making sure there are samples for uploading.
        IEnumerable<SampleActivity> samples = await _sampleActivitySerializer.DeserializeFromFileAsync(UploadContext.SerializedSampleFilePath).ConfigureAwait(false);
        Logger.LogDebug("{totalSampleCount} samples in total for uploading.", samples.Count());

        samples = GetValidSamples(UploadContext.TraceFilePath, samples);

        // Contract with Profiler Client: Serialize back so that the profiler knows to drop application insights custom events accordingly.
        await _sampleActivitySerializer.SerializeToFileAsync(samples, UploadContext.SerializedSampleFilePath).ConfigureAwait(false);

        return ShouldUploadTrace(UploadContext.UploadMode, samples.Count()) ? new UploadContextExtension(uploadContext) : null;
    }

    /// <summary>
    /// Validate samples, and return valid ones.
    /// </summary>
    internal protected IEnumerable<SampleActivity> GetValidSamples(string traceFilePath, IEnumerable<SampleActivity> samples)
    {
        Logger.LogTrace("Start to validate trace.");

        ITraceValidator validator = _traceValidatorFactory.Create(traceFilePath);
        int matchedSampleCount = 0;
        try
        {
            int gatheredSampleCount = samples.Count();
            samples = validator.Validate(samples).NullAsEmpty();
            matchedSampleCount = samples.Count();

            if (gatheredSampleCount != matchedSampleCount)
            {
                Logger.LogDebug("Matched sample count does not equal to gathered sample count. Expected: {gatheredCount}, Actual: {matchedCount}", gatheredSampleCount, matchedSampleCount);
            }
        }
        catch (ValidateFailedException ex)
        {
            Logger.LogWarning("Trace validation failed: {message}", ex.Message);
            Logger.LogDebug(ex, "Trace validation failure details.");
            if (ex.ShouldStopUploading)
            {
                // Return an empty list of sample activity is the best indicator that the uploading should be skipped without a major refactor.
                // However, we should looking for better ways to expression the intention.
                return Enumerable.Empty<SampleActivity>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected trace validation error.");
            throw;
        }

        Logger.LogTrace("Finished validate the trace.");
        return samples;
    }

    protected bool ShouldUploadTrace(UploadMode uploadMode, int validSampleCount)
    {
        Logger.LogTrace("{validSampleCount} sample(s) for uploading. Upload mode: {uploadMode}", validSampleCount, uploadMode);

        bool uploadTrace;
        switch (uploadMode)
        {
            // Normal case:
            case UploadMode.OnSuccess:
                uploadTrace = validSampleCount > 0;
                break;
            // Force upload, gives warning if the trace is invalid.
            case UploadMode.Always:
                uploadTrace = true;
                if (validSampleCount == 0)
                {
                    Logger.LogWarning("Force upload is set. Trace will still be uploaded.");
                }

                break;
            // Skip upload, tells the user where to find the trace.
            case UploadMode.Never:
                uploadTrace = false;
                Logger.LogInformation("Trace upload is skipped by configuration. Set PreserveTraceFile to true to persistent the trace.");
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized upload mode of: {uploadMode}. Please file an issue for investigation.");
        }

        return uploadTrace;
    }
}
