using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IEndpointProvider
{
    Uri GetEndpoint();
}