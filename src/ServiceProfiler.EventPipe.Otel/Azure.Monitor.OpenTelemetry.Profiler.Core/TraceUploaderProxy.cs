//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Contract.Agent.Profiler;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core;

internal class TraceUploaderProxy : ITraceUploader
{
    private readonly ILogger _logger;
    private readonly IServiceProfilerContext _context;
    private readonly IFile _fileService;
    private readonly IOutOfProcCallerFactory _uploaderCallerFactory;
    private readonly IUploadContextValidator _uploadContextValidator;
    private readonly ServiceProfilerOptions _userConfiguration;
    private readonly IUploaderPathProvider _uploaderPathProvider;

    public TraceUploaderProxy(
        IUploaderPathProvider uploaderPathProvider,
        IFile fileService,
        IOutOfProcCallerFactory uploaderCallerFactory,
        IServiceProfilerContext context,
        ILogger<TraceUploaderProxy> logger,
        IOptions<ServiceProfilerOptions> userConfiguration,
        IUploadContextValidator uploadContextValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _uploaderPathProvider = uploaderPathProvider ?? throw new ArgumentNullException(nameof(uploaderPathProvider));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _uploaderCallerFactory = uploaderCallerFactory ?? throw new ArgumentNullException(nameof(uploaderCallerFactory));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userConfiguration = userConfiguration.Value ?? throw new ArgumentNullException(nameof(userConfiguration));
        _uploadContextValidator = uploadContextValidator ?? throw new ArgumentNullException(nameof(uploadContextValidator));
    }

    public async Task<UploadContextModel?> UploadAsync(
        DateTimeOffset sessionId,
        string traceFilePath,
        string metadataFilePath,
        string? sampleFilePath,
        string? namedPipeName,
        string roleName,
        string triggerType,
        CancellationToken cancellationToken,
        string? uploaderFullPath = null)
    {
        if (string.IsNullOrEmpty(traceFilePath))
        {
            _logger.LogError("Trace file path is not specified.");
            return null;
        }

        if (!_fileService.Exists(traceFilePath))
        {
            _logger.LogError("Trace file {0} can't be found.", traceFilePath);
            return null;
        }

        if (string.IsNullOrEmpty(sampleFilePath) && string.IsNullOrEmpty(namedPipeName))
        {
            throw new InvalidOperationException($"'{nameof(sampleFilePath)}' and '{nameof(namedPipeName)}' cannot be null or empty at the same time");
        }

        if (_userConfiguration.UploadMode == UploadMode.Never)
        {
            _logger.LogInformation("Skip upload according to user configuration.");
            return null;
        }

        // Locate uploader.
        uploaderFullPath ??= _uploaderPathProvider.GetUploaderFullPath();

        if (string.IsNullOrEmpty(uploaderFullPath))
        {
            _logger.LogError("Trace Uploader is not provided or located.");
            return null;
        }

        _logger.LogInformation("Uploader to be used: {uploaderPath}", uploaderFullPath);

        // Upload is ready to go.
        UploadContextModel uploadContextModel = new()
        {
            AIInstrumentationKey = _context.AppInsightsInstrumentationKey,
            HostUrl = _context.StampFrontendEndpointUrl,
            StampId = string.Empty,
            SessionId = sessionId,
            TraceFilePath = traceFilePath,
            MetadataFilePath = metadataFilePath,
            PreserveTraceFile = _userConfiguration.PreserveTraceFile,
            SkipEndpointCertificateValidation = _userConfiguration.SkipEndpointCertificateValidation,
            UploadMode = _userConfiguration.UploadMode,
            SerializedSampleFilePath = sampleFilePath,
            PipeName = namedPipeName,
            RoleName = roleName,
            TriggerType = triggerType,
            Environment = _userConfiguration.UploaderEnvironment,
            TraceFileFormat = TraceFileFormat.Nettrace,
        };

        // Validation Failed
        string? message = _uploadContextValidator.Validate(uploadContextModel);
        if (!string.IsNullOrEmpty(message))
        {
            _logger.LogError(message);
            return null;
        }

        int exitCode = await CallUploadAsync(uploaderFullPath, uploadContextModel.ToString(), cancellationToken).ConfigureAwait(false);
        if (exitCode != 0)
        {
            // Upload Failed
            _logger.LogError("Trace upload failed. Exit code: {exitCode}", exitCode);
            return null;
        }

        // Upload succeeded.
        return uploadContextModel;
    }

    private Task<int> CallUploadAsync(string exePath, string args, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            const string executableName = "dotnet";
            try
            {
                IOutOfProcCaller uploaderCaller = _uploaderCallerFactory.Create(executableName, exePath + ' ' + args);
                // Assuming dotnet core SDK will always be installed.

                int exitCode = uploaderCaller.ExecuteAndWait(ProcessPriorityClass.BelowNormal);

                _logger.LogInformation("Call upload trace finished. Exit code: {exitCode}", exitCode);
                return exitCode;
            }
            catch (Exception ex) when (
                ex is InvalidOperationException ||
                ex is Win32Exception ||
                ex is ObjectDisposedException ||
                ex is PlatformNotSupportedException)
            {
                _logger.LogError(ex, "Failed to start uploader process. Make sure the application can access the executable of {executable} by having its directory on PATH environment variable. Check the message for details.", executableName);
                return int.MinValue;
            }
        }, cancellationToken);
    }
}
