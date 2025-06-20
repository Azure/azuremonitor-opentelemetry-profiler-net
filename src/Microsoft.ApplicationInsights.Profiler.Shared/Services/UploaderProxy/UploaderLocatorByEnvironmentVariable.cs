//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal class UploaderLocatorByEnvironmentVariable : UploadLocatorBase
{
    private const string UploaderPathEnvironmentVariablePattern = "%SP_UPLOADER_PATH%";
    private readonly IEnvironment _environment;
    public UploaderLocatorByEnvironmentVariable(
        IFile fileService,
        IEnvironment environment,
        ILogger<UploaderLocatorByEnvironmentVariable> logger
        )
        : base(fileService, logger)
    {
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public override int Priority => 0;

    protected override string? GetUploaderFullPath()
    {
        string uploaderPath = _environment.ExpandEnvironmentVariables(UploaderPathEnvironmentVariablePattern);
        Logger.LogDebug("UploaderPath by expanding environment variable of {variableName}: {expanded}", UploaderPathEnvironmentVariablePattern, uploaderPath);
        if (UploaderPathEnvironmentVariablePattern.Equals(uploaderPath, StringComparison.Ordinal))
        {
            Logger.LogDebug("Environment variable of {variableName} is not set. Falls back to default path.", UploaderPathEnvironmentVariablePattern);
            return null;
        }

        return uploaderPath;
    }
}
