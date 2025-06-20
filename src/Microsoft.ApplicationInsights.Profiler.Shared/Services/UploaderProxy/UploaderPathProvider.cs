using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.UploaderProxy;

internal class UploaderPathProvider : IUploaderPathProvider
{
    private readonly IEnumerable<IPrioritizedUploaderLocator> _uploaderLocators;

    public UploaderPathProvider(IEnumerable<IPrioritizedUploaderLocator> uploaderLocators)
    {
        _uploaderLocators = (uploaderLocators ?? throw new ArgumentNullException(nameof(uploaderLocators))).OrderBy(locator => locator.Priority);
    }

    public string GetUploaderFullPath()
    {
        string? uploaderFullPath = null;
        foreach (IPrioritizedUploaderLocator uploaderLocator in _uploaderLocators)
        {
            uploaderFullPath = uploaderLocator.Locate();
            if (!string.IsNullOrEmpty(uploaderFullPath))
            {
                break;
            }
        }

        if (string.IsNullOrEmpty(uploaderFullPath))
        {
            throw new InvalidOperationException("Uploader can't be located.");
        }

        return uploaderFullPath!;
    }

    public bool TryGetUploaderFullPath(out string uploaderFullPath)
    {
        uploaderFullPath = GetUploaderFullPath();
        return !string.IsNullOrEmpty(uploaderFullPath);
    }
}
