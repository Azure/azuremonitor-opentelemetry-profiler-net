//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Reflection;

namespace Microsoft.ApplicationInsights.Profiler.Core;

/// <inheritdoc />
internal class EndpointProviderMirror : IEndpointProvider
{
    private readonly MethodInfo _getEndpointMethod;
    private readonly object _endpointProvider;

    public EndpointProviderMirror(IOptions<TelemetryConfiguration> customerTelemetryConfigurationOptions)
    {
        Assembly applicationInsights = Assembly.GetAssembly(typeof(TelemetryClient));
        if (applicationInsights == null)
        {
            throw new NullReferenceException("Can't find Microsoft.ApplicationInsights assembly");
        }

        Type endpointProviderType = applicationInsights.GetType("Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints.EndpointProvider", throwOnError: true);
        _getEndpointMethod = endpointProviderType.GetMethod(nameof(GetEndpoint));

        _endpointProvider = Activator.CreateInstance(endpointProviderType);
    }

    public Uri GetEndpoint()
    {
        return (Uri)_getEndpointMethod.Invoke(_endpointProvider, [EndpointName.ProfilerEndpoint]);
    }
}