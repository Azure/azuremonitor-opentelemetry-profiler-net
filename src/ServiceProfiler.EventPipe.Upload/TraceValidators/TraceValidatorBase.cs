using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal abstract class TraceValidatorBase : ITraceValidator
    {
        public TraceValidatorBase(
            ILogger<TraceValidatorBase> logger,
            ITraceValidator? nextValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Next = nextValidator;
        }

        public IEnumerable<SampleActivity> Validate(IEnumerable<SampleActivity> samples)
        {
            _logger.LogTrace("Start validation.");
            IEnumerable<SampleActivity> validSamples = ValidateImp(samples);
            if (Next != null)
            {
                _logger.LogTrace("Move to the next validator for more validation");
                validSamples = Next.Validate(validSamples);
            }
            else
            {
                _logger.LogTrace("Done validation.");
            }

            return validSamples;
        }

        public ITraceValidator? Next { get; }

        protected abstract IEnumerable<SampleActivity> ValidateImp(IEnumerable<SampleActivity> samples);

        protected readonly ILogger _logger;
    }
}
