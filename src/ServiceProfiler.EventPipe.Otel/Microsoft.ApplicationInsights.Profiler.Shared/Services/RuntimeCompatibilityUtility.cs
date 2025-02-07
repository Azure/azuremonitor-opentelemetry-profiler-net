//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;
internal class RuntimeCompatibilityUtility : ICompatibilityUtility
{
    private readonly ILogger _logger;
    private readonly INetCoreAppVersion _netCoreAppVersion;
    private readonly IVersionProvider _versionProvider;

    public RuntimeCompatibilityUtility(
        INetCoreAppVersion netCoreAppVersion,
        IVersionProvider versionProvider,
        ILogger<RuntimeCompatibilityUtility> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
        _netCoreAppVersion = netCoreAppVersion ?? throw new ArgumentNullException(nameof(netCoreAppVersion));
    }

    /// <summary>
    /// Check whether the version passes the compatibility test or not.
    /// </summary>
    public (bool compatible, string reason) IsCompatible()
    {
        Version? runtimeVersion = _versionProvider.RuntimeVersion;
        if (runtimeVersion is not null)
        {
            // Shortcut:
            // .NET Core 3.0.0 or .NET 5.0 and above passes the compatibility test immediately
            if (runtimeVersion.Major == 3 || runtimeVersion.Major >= 5)
            {
                return (true, $"Good major version. Pass Runtime Compatibility test.");
            }

            // Otherwise: Some .NET Core 2.x platform returns 4.6.x for its framework version descriptions and that's where specific version check is required.
            Version? minVersion = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _logger.LogDebug("Checking compatibility for Linux platform.");
                minVersion = new Version(_netCoreAppVersion.NetCore2_1);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogDebug("Checking compatibility for Windows platform.");
                _logger.LogWarning("Service Profiler for Windows is in beta stage.");
                minVersion = new Version(_netCoreAppVersion.NetCore2_2);
            }

            _logger.LogDebug("Min: {0} Current: {1}", minVersion, runtimeVersion);

            if (minVersion is not null && runtimeVersion >= minVersion)
            {
                return (true, $"Pass Runtime Compatibility test.");
            }
            else
            {
                return (false, $"Runtime version {runtimeVersion} is smaller than the minimum version of {minVersion}.");
            }
        }

        return (false, "Can't parse version from the framework description.");
    }
}