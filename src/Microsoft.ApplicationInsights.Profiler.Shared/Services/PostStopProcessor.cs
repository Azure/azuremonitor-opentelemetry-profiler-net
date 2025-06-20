using Azure.Core;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.Auth;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Contract;
using Microsoft.ServiceProfiler.Orchestration;
using Microsoft.ServiceProfiler.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class PostStopProcessor : IPostStopProcessor
{
    private readonly UserConfigurationBase _serviceProfilerOptions;
    private readonly IUploaderPathProvider _uploaderPathProvider;
    private readonly ITraceUploader _traceUploader;
    private readonly INamedPipeClientFactory _namedPipeClientFactory;
    private readonly IAuthTokenProvider _authTokenProvider;
    private readonly ISerializationProvider _serializer;
    private readonly IServiceProfilerContext _serviceProfilerContext;
    private readonly IMetadataWriter _metadataWriter;
    private readonly IRoleNameSource _roleNameSource;
    private readonly ICustomEventsBuilder _customEventsBuilder;
    private readonly ILogger _logger;

    public PostStopProcessor(
        IUploaderPathProvider uploaderPathProvider,
        ITraceUploader traceUploader,
        IOptions<UserConfigurationBase> serviceProfilerOptions,
        INamedPipeClientFactory namedPipeClientFactory,
        IAuthTokenProvider authTokenProvider,
        ISerializationProvider serializer,
        IServiceProfilerContext serviceProfilerContext,
        IMetadataWriter metadataWriter,
        IRoleNameSource roleNameSource,
        ICustomEventsBuilder customEventsBuilder,
        ILogger<PostStopProcessor> logger)
    {
        _serviceProfilerOptions = serviceProfilerOptions?.Value ?? throw new ArgumentNullException(nameof(serviceProfilerOptions));
        _uploaderPathProvider = uploaderPathProvider ?? throw new ArgumentNullException(nameof(uploaderPathProvider));
        _traceUploader = traceUploader ?? throw new ArgumentNullException(nameof(traceUploader));
        _namedPipeClientFactory = namedPipeClientFactory ?? throw new ArgumentNullException(nameof(namedPipeClientFactory));
        _authTokenProvider = authTokenProvider ?? throw new ArgumentNullException(nameof(authTokenProvider));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _serviceProfilerContext = serviceProfilerContext ?? throw new ArgumentNullException(nameof(serviceProfilerContext));
        _metadataWriter = metadataWriter ?? throw new ArgumentNullException(nameof(metadataWriter));
        _roleNameSource = roleNameSource ?? throw new ArgumentNullException(nameof(roleNameSource));
        _customEventsBuilder = customEventsBuilder ?? throw new ArgumentNullException(nameof(customEventsBuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> PostStopProcessAsync(PostStopOptions e, CancellationToken cancellationToken)
    {
        _logger.LogTrace("Entering {name}", nameof(PostStopProcessAsync));

        // Execute the upload

        bool succeeded = await ExecuteUploadAsync(e, _serviceProfilerOptions.UploadMode, cancellationToken).ConfigureAwait(false);

        // Delete trace unless set to preserve.
        _logger.LogDebug("{preserveTraceFile} is set to {value}.", nameof(_serviceProfilerOptions.PreserveTraceFile), _serviceProfilerOptions.PreserveTraceFile);
        if (!_serviceProfilerOptions.PreserveTraceFile)
        {
            TryDeleteFiles(e.TraceFilePath);
        }
        else
        {
            _logger.LogInformation("Trace is preserved locally at: {tracePath}", e.TraceFilePath);
        }

        return succeeded;
    }

    private async Task<bool> ExecuteUploadAsync(PostStopOptions e, UploadMode uploadMode, CancellationToken cancellationToken)
    {
        if (uploadMode == UploadMode.Never)
        {
            _logger.LogInformation("Skip uploading. Upload mode: {mode}.", uploadMode);
            return false;
        }

        if (!_uploaderPathProvider.TryGetUploaderFullPath(out string uploaderFullPath))
        {
            _logger.LogError("Uploader is not found.");
            return false;
        }

        int sampleCount = e.Samples.Count();
        _logger.LogDebug("There are {sampleNumber} samples before validation.", sampleCount);
        if (sampleCount == 0 && uploadMode != UploadMode.Always)
        {
            return false;
        }

        e.UploaderFullPath = uploaderFullPath;

        int processId = CurrentProcessUtilities.GetId();

        string pipeName = Guid.NewGuid().ToString("D");
        Guid appId = Guid.Empty;

        Task namedPipeClientTask = Task.Run(async () =>
        {
            INamedPipeClientService namedPipeClient = _namedPipeClientFactory.CreateNamedPipeService();
            try
            {
                _logger.LogTrace("Waiting for connection of named pipe: {name}", pipeName);
                await namedPipeClient.ConnectAsync(pipeName, cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("Namedpipe {name} connected.", namedPipeClient.PipeName);

                _logger.LogTrace("Sending serialized samples.");
                await namedPipeClient.SendAsync(e.Samples).ConfigureAwait(false);
                _logger.LogTrace("Finished sending samples.");

                // Contract with Uploader: Only valid samples are written back.
                _logger.LogTrace("Waiting for the uploader to write back valid samples according to the contract.");
                // The uploader might need a while for sample validation before it returns the result. That is especially true under heavy loaded system.
                // Give it at least 10 minutes as a reasonable timeout. The user could choose to overwrite it with even longer time span by setting up operation timeout.
                double longerTimeoutMilliseconds = Math.Max(TimeSpan.FromMinutes(10).TotalMilliseconds, _serviceProfilerOptions.NamedPipe.DefaultMessageTimeout.TotalMilliseconds);
                e.Samples = (await namedPipeClient.ReadAsync<IEnumerable<SampleActivity>>(timeout: TimeSpan.FromMilliseconds(longerTimeoutMilliseconds)).ConfigureAwait(false)) ?? [];
                _logger.LogTrace("Finished loading valid samples.");

                // Sending the AccessToken for AAD authentication in case it is enabled.
                _logger.LogTrace("Sending access token");
                AccessToken accessToken = await _authTokenProvider.GetTokenAsync(cancellationToken: default).ConfigureAwait(false);
                await namedPipeClient.SendAsync(accessToken).ConfigureAwait(false);
                _logger.LogTrace("Finished sending access token for the uploader to use.");

                // Contract with Uploader: Return app id.
                _logger.LogTrace("Waiting for the uploader to write back valid appId as dataCube.");
                appId = await namedPipeClient.ReadAsync<Guid>().ConfigureAwait(false);
                _logger.LogTrace("Finished retrieving a valid appId (dataCube): {appId}", appId);
                if (appId == Guid.Empty)
                {
                    throw new InvalidOperationException($"Datacube {appId} is invalid.");
                }

                // Contract with Upload, sending additional data
                IPCAdditionalData additionalData = CreateAdditionalData(e.Samples.ToImmutableArray(), stampId: "%StampId%", e.SessionId, appId, e.ProfilerSource);
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Sending additional data for the uploader to use.");
                    if (_serializer.TrySerialize(additionalData, out string? serializedObject))
                    {
                        _logger.LogTrace("===== {serialized} =====", Environment.NewLine + serializedObject + Environment.NewLine);
                    }
                    else
                    {
                        if (e.Samples.Any())
                        {
                            _logger.LogWarning("Although there are valid samples, there's no additional data. Why?");
                        }
                        else
                        {
                            _logger.LogTrace("No additional data");
                        }
                    }
                }
                await namedPipeClient.SendAsync(additionalData, TimeSpan.FromMilliseconds(longerTimeoutMilliseconds), cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("Additional data sent.");
            }
            finally
            {
                (namedPipeClient as IDisposable)?.Dispose();
            }
        }, cancellationToken);

        Task<UploadContextModel?> uploadTask = UploadTraceAsync(e, processId, pipeName, cancellationToken);

        // Waiting for both task to finish.
        await Task.WhenAll(namedPipeClientTask, uploadTask).ConfigureAwait(false);

        UploadContextModel? uploadContext = uploadTask.Result;
        if (uploadContext != null)
        {
            // Trace is uploaded.
            int validSampleCount = e.Samples.Count();
            _logger.LogDebug("Sent {validSampleCount} valid custom events to AI. Valid sample count equals total sample count: {result}", validSampleCount, validSampleCount == sampleCount);
        }

        return true;
    }

    private IPCAdditionalData CreateAdditionalData(
        IReadOnlyCollection<SampleActivity> samples,
        string stampId, DateTimeOffset sessionId, Guid appId, IProfilerSource profilerSource)
        => new()
        {
            ConnectionString = _serviceProfilerContext.ConnectionString.ToString(),
            ServiceProfilerIndex = _customEventsBuilder.CreateServiceProfilerIndex(
            fileId: EnvironmentUtilities.CreateSessionId(),
            stampId: stampId,
            sessionId: sessionId,
            appId: appId,
            profilerSource: profilerSource),
            ServiceProfilerSamples = _customEventsBuilder.CreateServiceProfilerSamples(
            samples: samples,
            stampId: stampId,
            sessionId: sessionId,
            appId: appId),
        };

    /// <summary>
    /// Upload the trace file.
    /// </summary>
    /// <returns>Returns the upload context when upload succeeded. Returns null otherwise.</returns>
    private async Task<UploadContextModel?> UploadTraceAsync(
        PostStopOptions options, int processId, string namedPipeName, CancellationToken cancellationToken)
    {
        bool areOptionsSerialized = _serializer.TrySerialize(options, out string? serializedOptions);
        _logger.LogTrace(@"Trace session finished. Invoking upload. Args:
{arguments}", serializedOptions);

        string metadataPath = Path.ChangeExtension(options.TraceFilePath, "metadata");
        string machineName = _serviceProfilerContext.MachineName;
        await CreateMetadataAsync(options.Samples.Select(sample =>
            // At this point, we don't have a stamp id or the datacube yet, just use 'localstamp' as the stamp id.
            // Since the metadata file stays in the .etl.zip, it is not needed to locate the trace file.
            sample.ToArtifactLocationProperties("localstamp", processId, options.SessionId, Guid.Empty, machineName)
        ), metadataPath, cancellationToken).ConfigureAwait(false);

        return await _traceUploader.UploadAsync(
            options.SessionId,
            options.TraceFilePath,
            metadataPath,
            sampleFilePath: null,// Replaced by namedpipe name.
            namedPipeName,
            _roleNameSource.CloudRoleName,
            options.ProfilerSource.Source,
            cancellationToken,
            options.UploaderFullPath).ConfigureAwait(false);
    }

    private Task CreateMetadataAsync(IEnumerable<ArtifactLocationProperties> locations, string targetPath, CancellationToken cancellationToken)
        => _metadataWriter.WriteAsync(targetPath, locations, cancellationToken);

    private void TryDeleteFiles(params string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
        {
            return;
        }

        foreach (string filePath in filePaths)
        {
            try
            {
                File.Delete(filePath);

            }
            catch (Exception ex)
                when (ex is ArgumentException ||
                    ex is ArgumentNullException ||
                    ex is DirectoryNotFoundException ||
                    ex is IOException)
            {
                _logger.LogDebug(ex, "Fail to delete file at: {path}", filePath);
            }
        }
    }
}