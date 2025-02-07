using System;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal class UploaderLocatorInUserCache : UploadLocatorBase
{
    private readonly IUserCacheManager _userCacheManager;

    public override int Priority => 10;

    public UploaderLocatorInUserCache(
        IUserCacheManager userCacheManager,
        IFile fileService,
        ILogger<UploaderLocatorInUserCache> logger)
        : base(fileService, logger)
    {
        _userCacheManager = userCacheManager ?? throw new ArgumentNullException(nameof(userCacheManager));
    }

    protected override string? GetUploaderFullPath()
    {
        return Path.Combine(_userCacheManager.UploaderDirectory.FullName, TraceUploaderAssemblyName);
    }
}
