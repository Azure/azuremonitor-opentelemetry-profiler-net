using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;


/// <summary>
/// A service to run as a named pipe server.
/// </summary>
internal interface INamedPipeServerService : INamedPipeOperations
{
    /// <summary>
    /// Start a named pipe and wait for connection.
    /// </summary>
    Task WaitForConnectionAsync(string pipeName, CancellationToken cancellationToken);
}
