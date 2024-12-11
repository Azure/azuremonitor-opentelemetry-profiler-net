namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IConnectionStringParser
{
    bool TryGetValue(string key, out string? value);
}