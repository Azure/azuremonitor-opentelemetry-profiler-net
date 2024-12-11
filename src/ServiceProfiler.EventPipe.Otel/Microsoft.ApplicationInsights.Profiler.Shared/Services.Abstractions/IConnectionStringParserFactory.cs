namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IConnectionStringParserFactory
{
    IConnectionStringParser Create(string connectionString);
}