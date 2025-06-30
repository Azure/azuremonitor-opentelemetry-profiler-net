using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.RoleNames;

internal class UnknownRoleNameDetector(ILogger<UnknownRoleNameDetector> logger) : IRoleNameDetector
{
    private readonly ILogger _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

    public string? GetRoleName()
    {
        _logger.LogWarning("Role name could not be determined. This may occur during local debugging or due to a configuration issue. If this is unexpected, please open an issue at https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/issues with details about your environment for further investigation.");
        return "Unknown";
    }
}