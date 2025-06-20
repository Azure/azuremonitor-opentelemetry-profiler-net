namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IOutOfProcCallerFactory
{
    IOutOfProcCaller Create(string executable, string arguments);
}