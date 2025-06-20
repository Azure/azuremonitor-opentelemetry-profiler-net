namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IPostStopProcessorFactory
{
    IPostStopProcessor Create();
}