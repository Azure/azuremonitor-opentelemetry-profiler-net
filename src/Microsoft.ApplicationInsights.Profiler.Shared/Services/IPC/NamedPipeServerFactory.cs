using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC
{
    internal class NamedPipeServerFactory : INamedPipeServerFactory
    {
        private readonly NamedPipeOptions _options;
        private readonly IPayloadSerializer _payloadSerializer;
        private readonly ILoggerFactory _loggerFactory;

        public NamedPipeServerFactory(IOptions<NamedPipeOptions> namedPipeOptions, IPayloadSerializer payloadSerializer, ILoggerFactory loggerFactory)
        {
            _options = namedPipeOptions?.Value ?? new NamedPipeOptions();
            _payloadSerializer = payloadSerializer ?? throw new ArgumentNullException(nameof(payloadSerializer));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public INamedPipeServerService CreateNamedPipeService() =>
            new DuplexNamedPipeService(_options, _payloadSerializer, _loggerFactory.CreateLogger<DuplexNamedPipeService>());
    }
}
