using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;

/// <summary>
/// Common operations that a client or a server could do on a namedpipe.
/// </summary>
public interface INamedPipeOperations
{
    /// <summary>
    /// Gets the name of the pipe
    /// </summary>
    string? PipeName { get; }

    /// <summary>
    /// Send a typed message.
    /// </summary>
    Task SendAsync<T>(T message, TimeSpan timeout = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read a message into an object of a type.
    /// </summary>
    Task<T?> ReadAsync<T>(TimeSpan timeout = default, CancellationToken cancellationToken = default);
}
