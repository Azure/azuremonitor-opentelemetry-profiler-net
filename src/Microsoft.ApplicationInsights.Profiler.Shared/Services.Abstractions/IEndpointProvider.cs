using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// Provides the endpoint for the profiler service.
/// </summary>
internal interface IEndpointProvider
{
    Uri GetEndpoint();
}