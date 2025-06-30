using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal class ConvertTraceToEtlxValidator : TraceValidatorBase
    {
        private readonly string _traceFilePath;

        public ConvertTraceToEtlxValidator(
            string traceFilePath,
            ILogger<ConvertTraceToEtlxValidator> logger,
             ITraceValidator nextValidator)
            : base(logger, nextValidator)
        {
            if (string.IsNullOrEmpty(traceFilePath))
            {
                throw new ArgumentException($"'{nameof(traceFilePath)}' cannot be null or empty", nameof(traceFilePath));
            }

            _traceFilePath = traceFilePath;
        }

        protected override IEnumerable<SampleActivity> ValidateImp(IEnumerable<SampleActivity> samples)
        {
            string traceFilePath = _traceFilePath ?? throw new InvalidCastException($"'{nameof(_traceFilePath)}' cannot be null or empty in {nameof(ConvertTraceToEtlxValidator)}");

            if (!File.Exists(traceFilePath))
            {
                string message = $"File {traceFilePath} not found.";
                throw new ValidateFailedException(nameof(ConvertTraceToEtlxValidator), message, new FileNotFoundException(message), toStopUploading: true);
            }

            string? etlxPath = null;
            try
            {
                string workingDir = Path.GetDirectoryName(traceFilePath) ?? throw new InvalidOperationException("Working folder path can't be null.");

                etlxPath = Path.Combine(workingDir, Path.ChangeExtension(Path.GetRandomFileName(), ".etlx"));
                using TraceLog traceLog = new TraceLog(TraceLog.CreateFromEventPipeDataFile(_traceFilePath, etlxPath));
                return samples;
            }
            catch (Exception ex)
            {
                throw new ValidateFailedException(nameof(ConvertTraceToEtlxValidator), ex.Message, ex, toStopUploading: true);
            }
            finally
            {
                if (!string.IsNullOrEmpty(etlxPath))
                {
                    try
                    {
                        File.Delete(etlxPath);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        // Based on best effort. For the validator, it isn't the best idea to throw an exception in final block.
                        _logger.LogWarning(ex, "Deleting etlx file failed by {0} at {1}", nameof(ConvertTraceToEtlxValidator), etlxPath);
                    }
                }
            }
        }
    }
}
