//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts.CustomEvents;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Agent.Exceptions;
using Microsoft.ServiceProfiler.Agent.FrontendClient;
using Microsoft.ServiceProfiler.Contract;
using Microsoft.ServiceProfiler.Contract.Agent;
using Microsoft.ServiceProfiler.Utilities;
using ServiceProfiler.EventPipe.Upload;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.ApplicationInsights.Profiler.Uploader;

internal class TraceUploader : ITraceUploader
{
    private readonly IZipUtility _zipUtility;
    private readonly IProfilerFrontendClientBuilder _stampFrontendClientBuilder;
    private readonly IAppInsightsLogger _telemetryLogger;
    private readonly IOSPlatformProvider _osPlatformProvider;
    private readonly ITraceValidatorFactory _traceValidatorFactory;
    private readonly ISampleActivitySerializer _sampleActivitySerializer;
    private readonly ICustomEventsSender _customEventsSender;
    private readonly IBlobClientFactory _blobClientFactory;

    protected IAppProfileClientFactory AppProfileClientFactory { get; }
    protected UploadContext UploadContext { get; }
    protected IUploadContextValidator UploadContextValidator { get; }
    protected ILogger Logger { get; }

    public TraceUploader(
        IZipUtility zipUtility,
        IBlobClientFactory blobClientFactory,
        IProfilerFrontendClientBuilder stampFrontendClientBuilder,
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
        _stampFrontendClientBuilder = stampFrontendClientBuilder ?? throw new ArgumentNullException(nameof(stampFrontendClientBuilder));
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
        IProfilerFrontendClient? stampFrontendClient = null;
        try
        {
            stampFrontendClient = _stampFrontendClientBuilder.WithUploadContext(extendedContext).Build();
            string actualStampId = await stampFrontendClient.GetStampIdAsync(cancellationToken).ConfigureAwait(false);

            // The actual stamp id can not be null
            if (string.IsNullOrEmpty(actualStampId))
            {
                throw new InvalidOperationException(FormattableString.Invariant($"Stamp ID is null. This should not happen. Expected: {context.StampId}"));
            }

            // If the context.StampId is null, we will set it to the actual stamp id, so that it will not fail the comparison later.
            if (string.IsNullOrEmpty(context.StampId))
            {
                context.StampId = actualStampId;
            }

            // Compare the actual stamp id with the expected one.
            if (!string.Equals(actualStampId, context.StampId, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(FormattableString.Invariant($"Actual stamp-id of {actualStampId} is different than expected stamp-id: {context.StampId}"));
            }

            BlobAccessPass uploadPass = await stampFrontendClient.GetEtlUploadAccessAsync(
                context.SessionId,
                cancellationToken).ConfigureAwait(false);

            if (uploadPass == null)
            {
                throw new InvalidOperationException("Failed to get a pass to upload the trace file.");
            }

            Logger.LogDebug("Uri with SAS Token: {uploadPassValue}", uploadPass.GetUriWithSASToken().AbsoluteUri);
            BlobClient blob = _blobClientFactory.CreateBlobClient(uploadPass.GetUriWithSASToken());
            await blob.UploadAsync(zippedFilePath, cancellationToken).ConfigureAwait(false);

            // Update the blob metadata.
            Dictionary<string, string> metadata = CreateMetadata(extendedContext);
            await blob.SetMetadataAsync(metadata, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (await stampFrontendClient.ReportEtlUploadFinishAsync(uploadPass, cancellationToken).ConfigureAwait(false))
            {
                Logger.LogDebug("Blob upload finished @ {blobPath}.", blob?.BlobContainerName + '/' + blob?.Name);
                Logger.LogInformation(TelemetryConstants.TraceUploaded);
            }
            else
            {
                throw new InvalidOperationException("Failed to commit the uploaded etl file.");
            }

            _telemetryLogger.Flush();
        }
        catch (InstrumentationKeyInvalidException ikie)
        {
            Logger.LogError(ikie, "Instrumentation Key is not well formed or empty.");
        }
        finally
        {
            (stampFrontendClient as IDisposable)?.Dispose();

            if (!string.IsNullOrEmpty(zippedFilePath))
            {
                if (!context.PreserveTraceFile)
                {
                    // Try clean up and wish the best.

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


    private Dictionary<string, string> CreateMetadata(UploadContextExtension extendedContext)
    {
        UploadContext context = extendedContext.UploadContext;
        Dictionary<string, string> metadata = new()
        {
            [BlobMetadataConstants.DataCubeMetaName] = BlobMetadata.GetDataCubeNameString(extendedContext.VerifiedAppId),
            // Notice, the machine name on the metadata needs to include the iis name suffix when running in antares.
            // Otherwise, the blob won't be located and approved by the frontend.
            [BlobMetadataConstants.MachineNameMetaName] = EnvironmentUtilities.MachineName,
            [BlobMetadataConstants.StartTimeMetaName] = TimestampContract.TimestampToString(context.SessionId),
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
            Logger.LogError(ex, "Expected trace validation error.");
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
