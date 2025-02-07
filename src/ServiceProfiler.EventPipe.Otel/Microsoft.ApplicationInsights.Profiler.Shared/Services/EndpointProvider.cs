using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Contract;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class EndpointProvider : IEndpointProvider
{
    private readonly UserConfigurationBase _options;

    public EndpointProvider(IOptions<UserConfigurationBase> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Uri GetEndpoint()
    {
        // First priorty of user overwrites
        if (_options.Endpoint is not null)
        {
            return new Uri(_options.Endpoint, UriKind.Absolute);
        }

        // TODO: Get endpoint from the connection string when available.

        // Fallback to gloal default settings
        return new Uri(FrontendEndpoints.ProdGlobal, UriKind.Absolute);


        //         string endpointByConnectionString = endpointProvider.GetEndpoint(EndpointName.ProfilerEndpoint)?.AbsoluteUri;
        // string endpointByUserConfiguration = userConfiguration.Value?.Endpoint;
        // if (string.IsNullOrEmpty(endpointByConnectionString) && string.IsNullOrEmpty(endpointByUserConfiguration))
        // {
        //     // Either are set, use default;
        //     StampFrontendEndpointUrl = new Uri(FrontendEndpoints.ProdGlobal, UriKind.Absolute);
        // }
        // else if (!string.IsNullOrEmpty(endpointByUserConfiguration))
        // {
        //     // Either set by user configuration only or user configuration takes precedence.
        //     StampFrontendEndpointUrl = new Uri(endpointByUserConfiguration, UriKind.Absolute);
        // }
        // else
        // {
        //     // When it runs here, it means there's no userconfiguration but there's connection string for the endpoint.
        //     StampFrontendEndpointUrl = new Uri(endpointByConnectionString, UriKind.Absolute);
        // }
        //     }
    }
}