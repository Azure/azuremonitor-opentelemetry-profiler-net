using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IVersionProvider
{
    Version? RuntimeVersion { get; }
}