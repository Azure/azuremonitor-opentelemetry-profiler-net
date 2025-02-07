using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class ConnectionStringParser : IConnectionStringParser
{
    public static class Keys
    {
        public const string InstrumentationKey = "InstrumentationKey";
        public const string IngestionEndpoint = "IngestionEndpoint";
        public const string LiveEndpoint = "LiveEndpoint";
        public const string ApplicationId = "ApplicationId";

        // See https://learn.microsoft.com/azure/azure-monitor/app/azure-ad-authentication?tabs=net#token-audience for details.
        public const string AadAudience = "AadAudience";
    }

    private readonly string _connectionString;
    private readonly ILogger _logger;

    public ConnectionStringParser(
        string connectionString,
        ILogger<ConnectionStringParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));
        }
        _connectionString = connectionString;
    }

    public bool TryGetValue(string key, out string? value)
    {
        value = null;
        _logger.LogTrace("Parsing connection string: {connectionString}", _connectionString);

        string[] sections = _connectionString.Split([';'], StringSplitOptions.RemoveEmptyEntries);
        foreach (string section in sections)
        {
            _logger.LogTrace("Current section: {section}", section);
            string[] keyValues = section.Split(['='], StringSplitOptions.RemoveEmptyEntries);
            if (keyValues.Length != 2)
            {
                _logger.LogDebug("Unexpected token count. Expects 2. Actual: {count}", keyValues.Length);
                continue;
            }

            _logger.LogDebug("Parsing. Key: {key}, Value: {value}", keyValues[0], keyValues[1]);

            if (string.Equals(keyValues[0], key, StringComparison.OrdinalIgnoreCase))
            {
                value = keyValues[1];
                return true;
            }
        }

        return false;
    }

    public override string ToString()
    {
        return _connectionString;
    }
}