using System;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class VersionProvider : IVersionProvider
{
    private readonly ILogger _logger;

    public VersionProvider(string version, ILogger<IVersionProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        if (string.IsNullOrEmpty(version))
        {
            throw new ArgumentException($"'{nameof(version)}' cannot be null or empty", nameof(version));
        }

        RuntimeVersion = Parse(version);
    }

    public Version? RuntimeVersion { get; }

    private Version? Parse(string versionDescription)
    {
        TimeSpan matchTimeout = TimeSpan.FromSeconds(2);
        Regex regexTraditional = new Regex(@"^.*\s+(\d+\.\d+\.\d+\.\d+)$", RegexOptions.Compiled, matchTimeout);
        // Refer to https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#improved-net-core-version-apis
        Regex regexImproved = new Regex(@"^\.NET (Core )?(\d+\.\d+\.\d+).*", RegexOptions.Compiled, matchTimeout);

        Match? match = null;
        if (regexTraditional.IsMatch(versionDescription))
        {
            match = regexTraditional.Match(versionDescription);
        }
        else if (regexImproved.IsMatch(versionDescription))
        {
            match = regexImproved.Match(versionDescription);
        }

        int groupCount = match?.Groups?.Count ?? 0;
        if (match != null && groupCount >= 2)
        {
            // Last matched as version part.
            string versionString = match.Groups[groupCount - 1].Value;
            if (Version.TryParse(versionString, out Version version))
            {
                return version;
            }
            else
            {
                _logger.LogWarning("Version part of {versionString} can't be parsed.", versionString);
            }

            _logger.LogWarning("Framework description of {description} has unexpected matching group by regular expression of {expressionForOldFormat} or {expressionForNewFormat}.",
                    versionDescription, regexTraditional, regexImproved);
            return null;
        }
        else
        {
            _logger.LogWarning("Framework description of {0} is not well formed.", versionDescription);
            return default;
        }
    }
}
