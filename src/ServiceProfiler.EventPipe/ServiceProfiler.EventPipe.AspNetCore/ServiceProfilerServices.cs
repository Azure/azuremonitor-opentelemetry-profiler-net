//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore;

/// <summary>
/// Global access point to access Service Profiler related services.
/// </summary>
internal class ServiceProfilerServices
{
    public event EventHandler<EventArgs>? ServicesInitialized;

    public IServiceCollection? Services
    {
        get { return _services; }
        internal set
        {
            if (_services != value)
            {
                _services = value;
                this.ServicesInitialized?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private IServiceCollection? _services;
}
