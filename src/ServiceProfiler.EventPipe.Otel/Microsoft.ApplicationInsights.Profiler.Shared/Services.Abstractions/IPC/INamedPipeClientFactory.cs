namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;

internal interface INamedPipeClientFactory
{
    INamedPipeClientService CreateNamedPipeService();
}
