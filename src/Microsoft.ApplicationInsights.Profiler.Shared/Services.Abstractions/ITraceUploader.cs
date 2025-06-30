//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface ITraceUploader
{
    /// <summary>
    /// Upload the trace file by given session id and file path.
    /// </summary>
    /// <param name="sessionId">The session id.</param>
    /// <param name="traceFilePath">The trace file path.</param>
    /// <param name="metadataFilePath">The file path for a file of metadata.</param>
    /// <param name="sampleFilePath">The file path to the serialized samples.</param>
    /// <param name="namedPipeName">Name for the named pipe between the agent and the uploader.</param>
    /// <param name="roleName">The role name (if known) for the app.</param>
    /// <param name="triggerType">The type of trigger that caused collection of the trace. Also known as the "profiler source".</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="uploaderFullPath">Optionally provides full path to the uploader. If this isn't provided, the default uploader locators will be used to locate one.</param>
    /// <returns>Returns the upload context used when succeeded. Otherwise, returns null.</returns>
    Task<UploadContextModel?> UploadAsync(
        DateTimeOffset sessionId,
        string traceFilePath,
        string metadataFilePath,
        string? sampleFilePath,
        string? namedPipeName,
        string roleName,
        string triggerType,
        CancellationToken cancellationToken,
        string? uploaderFullPath = null);
}
