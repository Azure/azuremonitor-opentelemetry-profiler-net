using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceProfiler.EventPipe.Upload;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    class HostedUploaderService : IHostedService
    {
        private int? _exitCode;
        private readonly TraceUploaderFactory _traceUploaderFactory;
        private readonly IHostApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;

        public HostedUploaderService(
            TraceUploaderFactory traceUploaderFactory,
            IHostApplicationLifetime applicationLifetime,
            ILogger<HostedUploaderService> logger
            )
        {
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _traceUploaderFactory = traceUploaderFactory ?? throw new System.ArgumentNullException(nameof(traceUploaderFactory));
            _applicationLifetime = applicationLifetime;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _applicationLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogTrace("Start uploader");
                        TraceUploader traceUploader = _traceUploaderFactory.Create();
                        await traceUploader.UploadAsync(cancellationToken).ConfigureAwait(false);
                        _exitCode = 0;
                        _logger.LogTrace("Finish uploader");
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        _logger.LogError(ex, "Unhandled exception");
                        _exitCode = -1;
                    }
                    finally
                    {
                        // Stop the application when the work is done.
                        _applicationLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogTrace("Exit uploader.");

            Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
            return Task.CompletedTask;
        }
    }
}
