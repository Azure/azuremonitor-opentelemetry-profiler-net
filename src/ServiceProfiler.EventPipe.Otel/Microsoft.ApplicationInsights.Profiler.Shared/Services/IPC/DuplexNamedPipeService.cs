using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;

/// <summary>
/// A service implementation for namedpipe.
/// This class contains shared implementations for both client and server.
/// It is recommended to use the specific interface like INamedPipeServer or INamedPipeClient instead of to this class directly.
/// </summary>
/// <remarks>
/// When using with Dependency Injection container, it is recommended to register either INamedPipeServerService or INamedPipeClientService.
/// For example, do either
/// services.AddSingleton{INamedPipeServerService, DuplexNamedPipeService};
/// or
/// services.AddSingleton{INamedPipeClientService, DuplexNamedPipeService};
/// but do not register both at the same time.
/// </remarks>
internal sealed class DuplexNamedPipeService : INamedPipeServerService, INamedPipeClientService, IDisposable
{
    private readonly SemaphoreSlim _threadSafeLock = new(1, 1);
    private NamedPipeRole _currentMode = NamedPipeRole.NotSpecified;
    private PipeStream? _pipeStream;
    private readonly NamedPipeOptions _options;
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ILogger _logger;

    public DuplexNamedPipeService(
        NamedPipeOptions options,
        IPayloadSerializer payloadSerializer,
        ILogger<DuplexNamedPipeService> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _payloadSerializer = payloadSerializer ?? throw new ArgumentNullException(nameof(payloadSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string? PipeName { get; private set; }

    /// <inheritdoc />
    public async Task WaitForConnectionAsync(string pipeName, CancellationToken cancellationToken)
    {
        _logger.LogTrace("{methodName} on namedpipe: {pipeName}", nameof(WaitForConnectionAsync), pipeName);
        if (string.IsNullOrEmpty(pipeName))
        {
            throw new ArgumentException($"'{nameof(pipeName)}' cannot be null or empty.", nameof(pipeName));
        }

        try
        {
            using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource(_options.ConnectionTimeout);
            using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutCancellationTokenSource.Token, cancellationToken);
            cancellationToken = linkedCancellationTokenSource.Token;

            await _threadSafeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            switch (_currentMode)
            {
                case NamedPipeRole.Client:
                    throw new InvalidOperationException("Can't wait for connection on a client.");
                case NamedPipeRole.Server:
                    throw new InvalidOperationException("Can't setup a server for a second time.");
                case NamedPipeRole.NotSpecified:
                default:
                    // Good to proceed forward
                    break;
            }

            _currentMode = NamedPipeRole.Server;
            PipeName = pipeName;
            _logger.LogDebug("Named pipe stream initialization done. Role: {role}, PipeName: {pipeName}", _currentMode, PipeName);

            NamedPipeServerStream serverStream = new NamedPipeServerStream(PipeName, PipeDirection.InOut, maxNumberOfServerInstances: 1, transmissionMode: PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _pipeStream = serverStream;

            _logger.LogTrace("NamedPipe {} waiting for connection...", _currentMode);
            await serverStream.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("NamedPipe {} closed.", _currentMode);
        }
        finally
        {
            _threadSafeLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task ConnectAsync(string pipeName, CancellationToken cancellationToken)
    {
        _logger.LogTrace("{methodName} on namedpipe: {pipeName}", nameof(ConnectAsync), pipeName);

        if (string.IsNullOrEmpty(pipeName))
        {
            throw new ArgumentException($"'{nameof(pipeName)}' cannot be null or empty.", nameof(pipeName));
        }

        try
        {
            using CancellationTokenSource timeoutCancellationTokenSource = new CancellationTokenSource(_options.ConnectionTimeout);
            using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
            // Becomes token with timeout
            cancellationToken = linkedCancellationTokenSource.Token;

            await _threadSafeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            switch (_currentMode)
            {
                case NamedPipeRole.Client:
                    throw new InvalidOperationException("A connection is already established.");
                case NamedPipeRole.Server:
                    throw new InvalidOperationException("Can't connect to another server from a server.");
                case NamedPipeRole.NotSpecified:
                default:
                    break;
            }

            PipeName = pipeName;
            _currentMode = NamedPipeRole.Client;
            _logger.LogDebug("Named pipe stream initialization done. Role: {role}, PipeName: {pipeName}", _currentMode, PipeName);
            NamedPipeClientStream clientStream = new NamedPipeClientStream(serverName: ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            _pipeStream = clientStream;

            _logger.LogTrace("NamedPipe {role} trying to connect ...", _currentMode);
            await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogTrace("NamedPipe {role} connected.", _currentMode);

        }
        finally
        {
            _threadSafeLock.Release();
        }
    }

    /// <inheritdoc />
    public Task SendAsync<T>(T message, TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        VerifyModeIsSpecified();
        if (_payloadSerializer.TrySerialize(message, out string? payload))
        {
            _logger.LogTrace("Sending payload over named pipe: {payload}", payload);
            return SendMessageAsync(payload!, timeout, cancellationToken);
        }

        throw new UnsupportedPayloadTypeException("Can't serialize the message object.");
    }

    /// <inheritdoc />
    public async Task<T?> ReadAsync<T>(TimeSpan timeout = default, CancellationToken cancellationToken = default)
    {
        VerifyModeIsSpecified();
        string payload = await ReadMessageAsync(timeout, cancellationToken).ConfigureAwait(false);
        _logger.LogTrace("Reading payload from named pipe: {payload}", payload);
        if (_payloadSerializer.TryDeserialize<T>(payload, out T? result))
        {
            return result;
        }

        throw new UnsupportedPayloadTypeException("Can't deserialize message over the named pipe.");
    }

    private async Task<string> ReadMessageAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        VerifyModeIsSpecified();
        timeout = ConfigureReadWriteTimeout(timeout);

        using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);
        cancellationToken = linkedCancellationTokenSource.Token;

        Task timeoutTask = Task.Delay(timeout, cancellationToken);
        Task<string> readlineTask = Task.Run(async () =>
        {
            using (StreamReader reader = new StreamReader(_pipeStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: -1, leaveOpen: true))
            {
                return await reader.ReadLineAsync().ConfigureAwait(false);
            }
        });

        await Task.WhenAny(timeoutTask, readlineTask).ConfigureAwait(false);

        if (!readlineTask.IsCompleted)
        {
            throw new TimeoutException($"Can't finish reading message within given timeout: {timeout.TotalMilliseconds}ms");
        }

        return readlineTask.Result;
    }

    private async Task SendMessageAsync(string message, TimeSpan timeout, CancellationToken cancellationToken)
    {
        VerifyModeIsSpecified();
        VerifyMessageIsTransmitable(message);

        timeout = ConfigureReadWriteTimeout(timeout);
        using CancellationTokenSource timeoutSource = new CancellationTokenSource(timeout);
        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutSource.Token, cancellationToken);
        cancellationToken = linkedCancellationTokenSource.Token;

        Task timeoutTask = Task.Delay(timeout, cancellationToken);
        Task writelineTask = Task.Run(async () =>
        {
            using (StreamWriter writer = new StreamWriter(_pipeStream, encoding: Encoding.UTF8, bufferSize: -1, leaveOpen: true))
            {
                await writer.WriteLineAsync(message).ConfigureAwait(false);
            }
        }, cancellationToken);
        await Task.WhenAny(timeoutTask, writelineTask).ConfigureAwait(false);

        if (!writelineTask.IsCompleted)
        {
            throw new TimeoutException($"Can't finish writing message within given timeout: {timeout.TotalMilliseconds}ms");
        }
    }

    private TimeSpan ConfigureReadWriteTimeout(TimeSpan timeout)
    {
        // Use the default value
        if (timeout == default)
        {
            timeout = _options.DefaultMessageTimeout;
        }
        // Can't continue if the default vaule is set to 0.
        if (timeout == default)
        {
            throw new InvalidOperationException($"The current timeout span is not accepted: {timeout}");
        }

        return timeout;
    }

    private void VerifyMessageIsTransmitable(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new UnsupportedPayloadContentException("Can't transfer emtpy message over the namedpipe.");
        }

        if (message.Contains(Environment.NewLine))
        {
            throw new UnsupportedPayloadContentException("Do not support newline in message over the namedpipe.");
        }
    }

    public void Dispose()
    {
        _threadSafeLock?.Dispose();

        _pipeStream?.Dispose();
        _pipeStream = null;
    }

    private void VerifyModeIsSpecified()
    {
        if (_currentMode is NamedPipeRole.NotSpecified)
        {
            throw new InvalidOperationException("Pipe mode requires to be set before operations. Either wait for connection as a server or connect to a server as a client before reading or writing messages.");
        }
    }
}
