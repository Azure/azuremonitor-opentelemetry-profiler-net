using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.ApplicationInsights.Profiler.AspNetCore
{
    internal class ConfigureUserConfiguration : IConfigureOptions<UserConfiguration>
    {
        private const string ServiceProfilerSectionName = "ServiceProfiler";

        private readonly IConfiguration? _configuration;
        private readonly Action<UserConfiguration>? _overwriter;

        private readonly ILogger _logger;
        private ISerializationProvider _serializer { get; }
        public ConfigureUserConfiguration(
            IConfiguration? configuration,
            Action<UserConfiguration>? overwriter,
            ISerializationProvider serializer,
            ILogger<ConfigureUserConfiguration> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            // Nullable:
            _configuration = configuration;
            _overwriter = overwriter;
        }
        public void Configure(UserConfiguration options)
        {
            _logger.LogDebug("Configure user configuration.");
            if (_configuration != null)
            {
                _configuration.Bind(ServiceProfilerSectionName, options);
            }
            else
            {
                _logger.LogDebug("No configuration to use for user configuration. Keep defaults.");
            }

            _overwriter?.Invoke(options);

            if (_serializer.TrySerialize(options, out string? serializedOptions))
            {
                _logger.LogTrace(serializedOptions);
            }
        }
    }
}
