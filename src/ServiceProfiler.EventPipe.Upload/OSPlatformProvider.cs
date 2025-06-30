//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Microsoft.ServiceProfiler.Contract;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    public class OSPlatformProvider : IOSPlatformProvider
    {

        public OSPlatformProvider(ILogger<OSPlatformProvider> logger)
        {
            _logger = logger;
        }

        public string GetOSPlatformDescription()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatforms.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatforms.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatforms.OSX;
            }
            else
            {
                string error = "Unrecognized OS platform.";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }
        }

        private readonly ILogger _logger;
    }
}
