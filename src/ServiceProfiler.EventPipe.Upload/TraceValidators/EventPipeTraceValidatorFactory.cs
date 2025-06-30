using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    internal class EventPipeTraceValidatorFactory : ITraceValidatorFactory
    {
        public EventPipeTraceValidatorFactory(IServiceProvider serviceProvider, ILogger<EventPipeTraceValidatorFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
        }

        public ITraceValidator Create(string traceFilePath)
        {
            _logger.LogTrace("Creating trace validator chain.");
            return new ConvertTraceToEtlxValidator(traceFilePath, GetLogger<ConvertTraceToEtlxValidator>(),
                new ActivityListValidator(traceFilePath, GetLogger<ActivityListValidator>(), null)
            );
        }

        private ILogger<T> GetLogger<T>()
        {
            return _serviceProvider.GetRequiredService<ILogger<T>>();
        }

        private readonly IServiceProvider _serviceProvider;
        private ILogger _logger;
    }
}
