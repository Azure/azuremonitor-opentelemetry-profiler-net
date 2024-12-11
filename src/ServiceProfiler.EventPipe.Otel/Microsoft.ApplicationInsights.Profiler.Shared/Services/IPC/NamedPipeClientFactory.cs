using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC
{
    internal class NamedPipeClientFactory : INamedPipeClientFactory
    {
        private readonly NamedPipeOptions _options;
        private readonly IPayloadSerializer _payloadSerializer;
        private readonly ILoggerFactory _loggerFactory;

        public NamedPipeClientFactory(IOptions<UserConfigurationBase> userConfiguration, IPayloadSerializer payloadSerializer, ILoggerFactory loggerFactory)
        {
            _options = userConfiguration?.Value?.NamedPipe ?? throw new ArgumentNullException(nameof(userConfiguration));
            _payloadSerializer = payloadSerializer ?? throw new ArgumentNullException(nameof(payloadSerializer));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public INamedPipeClientService CreateNamedPipeService() =>
            new DuplexNamedPipeService(_options, _payloadSerializer, _loggerFactory.CreateLogger<DuplexNamedPipeService>());
    }
}
