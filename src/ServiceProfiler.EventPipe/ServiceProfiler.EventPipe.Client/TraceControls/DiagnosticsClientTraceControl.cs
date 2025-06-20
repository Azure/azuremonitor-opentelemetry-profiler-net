using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceProfiler.Common.Utilities;

namespace Microsoft.ApplicationInsights.Profiler.Core.TraceControls
{
    internal sealed class DiagnosticsClientTraceControl : ITraceControl, IDisposable
    {
        private readonly string _typeName;
        private static SemaphoreSlim _singleTraceSessionHandle = new SemaphoreSlim(1, 1);
        private readonly DiagnosticsClientProvider _diagnosticsClientProvider;
        private readonly DiagnosticsClientTraceConfiguration _configuration;
        private readonly IThreadUtilities _threadUtilities;
        private readonly UserConfiguration _userConfiguration;
        private readonly ILogger _logger;
        private EventPipeSession? _currentSession;
        private Task? _traceFileWritingTask;
        private const string TimeoutMessage = "Timed out waiting for semaphore.";

        public DiagnosticsClientTraceControl(
            DiagnosticsClientProvider diagnosticsClientProvider,
            DiagnosticsClientTraceConfiguration configuration,
            IThreadUtilities threadUtilities,
            IOptions<UserConfiguration> userConfiguration,
            ILogger<DiagnosticsClientTraceControl> logger
            )
        {
            _typeName = this.GetType().Name;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userConfiguration = userConfiguration?.Value ?? throw new ArgumentNullException(nameof(userConfiguration));
            _diagnosticsClientProvider = diagnosticsClientProvider ?? throw new ArgumentNullException(nameof(diagnosticsClientProvider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _threadUtilities = threadUtilities ?? throw new ArgumentNullException(nameof(threadUtilities));
        }

        public DateTime? SessionStartUTC { get; private set; }

        public async Task DisableAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("[{typeName}] Entering {methodName}()...", _typeName, nameof(DisableAsync));

            try
            {
                await StopProfilerSessionAsync(disposeEventSessionImmediately: false, cancellationToken).ConfigureAwait(false);

                if (_traceFileWritingTask is not null)
                {
                    await _traceFileWritingTask.ConfigureAwait(false);
                }
                else
                {
                    _logger.LogError("Trace file writing task is null upon disabling tracing. This should not happen.");
                }
            }
            finally
            {
                DisposeEventPipeSession();
            }
        }


        public Task EnableAsync(string traceFilePath, CancellationToken cancellationToken)
        {
            _logger.LogTrace("Entering {typeName}.{methodName}()", _typeName, nameof(EnableAsync));
            if (_singleTraceSessionHandle.Wait(TimeSpan.FromSeconds(10)))
            {
                try
                {
                    if (_currentSession != null)
                    {
                        throw new InvalidOperationException("Only 1 session at a time is supported.");
                    }

                    SessionStartUTC = DateTime.UtcNow;

                    int pid = default;
                    try
                    {
                        pid = CurrentProcessUtilities.GetId();
                    }
                    catch (InvalidOperationException ex) when (!_userConfiguration.AllowsCrash)
                    {
                        _logger.LogError(ex, "Failed getting process id. Profiler won't start.");
                        return Task.CompletedTask;
                    }

                    _currentSession = _diagnosticsClientProvider.GetDiagnosticsClient().StartEventPipeSession(
                        _configuration.BuildEventPipeProviders(),
                        requestRundown: _configuration.RequestRundown,
                        circularBufferMB: _configuration.CircularBufferMB);

                    _logger.LogTrace("Triggering writing to trace file");
                    _traceFileWritingTask = StartWriteAsync(traceFilePath, _currentSession.EventStream);
                    _logger.LogTrace("{name} enabled.", _typeName);
                }
                finally
                {
                    _singleTraceSessionHandle.Release();
                }
                return Task.CompletedTask;
            }
            else
            {
                throw new TimeoutException(TimeoutMessage);
            }
        }

        private async Task StopProfilerSessionAsync(bool disposeEventSessionImmediately, CancellationToken cancellationToken)
        {
            if (_singleTraceSessionHandle.Wait(TimeSpan.FromSeconds(10), cancellationToken))
            {
                try
                {
                    if (_currentSession != null)
                    {
                        _logger.LogTrace("Trigger stopping an EventPipe session . . .");
                        TimeSpan timeout = TimeSpan.FromHours(1);
                        try
                        {
                            await _threadUtilities.CallWithTimeoutAsync(_currentSession.Stop, timeout).ConfigureAwait(false);
                        }
                        catch (TimeoutException ex)
                        {
                            _logger.LogInformation(ex, "Can't stop EventPipe in given period: {timeout}. Profiler is failing. Please restart your service. This might caused by a known bug. Refer to https://aka.ms/ep-sp/bugs/135 for more details.", timeout);
                        }
                        catch (ServerNotAvailableException ex)
                        {
                            _logger.LogInformation(ex, "If the application is shutting down, it is safe to ignore it. Refer to https://aka.ms/ep-sp/bugs/117 for more details.");
                        }

                        _logger.LogTrace("The eventpipe session is stopped. ");

                        if (disposeEventSessionImmediately)
                        {
                            DisposeEventPipeSession();
                        }
                    }

                    _logger.LogTrace("[{typeName}] Disabled.", _typeName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected exception stopping session.");
                    throw;
                }
                finally
                {
                    _singleTraceSessionHandle.Release();
                }
            }
            else
            {
                throw new TimeoutException(TimeoutMessage);
            }
        }

        private async Task StartWriteAsync(string traceFilePath, Stream readFrom, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogTrace("Starts to write to file: {fileName}", traceFilePath);
                using Stream writeTo = new FileStream(traceFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                _logger.LogTrace("Start writing file ...");
                await readFrom.CopyToAsync(writeTo, bufferSize: 81920, cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogTrace("Finish writing file.");
            }
            catch (ObjectDisposedException ex)
            {
                _ = CurrentProcessUtilities.TryGetId(out int? pid);

                string? eventPipeIPCFullPath = null;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    eventPipeIPCFullPath = FormattableString.Invariant($@"\\.pipe\dotnet-diagnostic-{pid}");
                }
                else
                {
                    try
                    {
                        // Try best match on other platforms
                        string ipcRootPath = Path.GetTempPath();
                        eventPipeIPCFullPath = Directory.EnumerateFiles(ipcRootPath, $"dotnet-diagnostic-{pid}-*-socket", SearchOption.TopDirectoryOnly)
                            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                            .FirstOrDefault();
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogDebug("No EventPipe pipeline file.");
                    }
                }

                if (string.IsNullOrEmpty(eventPipeIPCFullPath) || !File.Exists(eventPipeIPCFullPath))
                {
                    // The IPC file doesn't exist, there isn't too much to be done. Log a warning for scenario analysis.
                    _logger.LogWarning(ex, "Profiler service is closed. This happens when application is shutting down.");
                }
                else
                {
                    // If the IPC file exists, it is unexpected ObjectDisposedException, rethrow for logging errors.
                    throw;
                }
            }
            // No hanging upon exception
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write the trace file.");
                throw;
            }
        }

        private void DisposeEventPipeSession()
        {
            _logger.LogTrace("[{typeName}] Disposing eventpipe session.", _typeName);
            _currentSession?.Dispose();
            _currentSession = null;
            _logger.LogTrace("[{typeName}] Eventpipe session disposed.", _typeName);
        }

        public void Dispose()
        {
            DisposeEventPipeSession();
        }
    }
}
