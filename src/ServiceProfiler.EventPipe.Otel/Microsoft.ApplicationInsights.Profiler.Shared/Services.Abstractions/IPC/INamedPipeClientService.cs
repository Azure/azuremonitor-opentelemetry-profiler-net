using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;

/// <summary>
/// A simplied wrapper for NamedPipe client
/// </summary>
public interface INamedPipeClientService : INamedPipeOperations
{
    /// <summary>
    /// Connects to the server with timeout.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the connection.</param>
    Task ConnectAsync(string pipeName, CancellationToken cancellationToken);
}
