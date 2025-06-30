using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class AggregatedRoleNameSource : IRoleNameSource
{
    private readonly ILogger<AggregatedRoleNameSource> _logger;

    public AggregatedRoleNameSource(
        IEnumerable<IRoleNameDetector> roleNameDetectors,
        ILogger<AggregatedRoleNameSource> logger
        )
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        if(!roleNameDetectors.Any())
        {
            _logger.LogWarning("There's no role name detector registered. The role name will be empty. This should not happen.");
        }

        foreach (IRoleNameDetector roleNameDetector in roleNameDetectors)
        {
            string roleName = roleNameDetector.GetRoleName()?? string.Empty;
            _logger.LogDebug("Role name detector {detector} returned role name: {roleName}", roleNameDetector.GetType().Name, roleName);

            if (string.IsNullOrEmpty(roleName))
            {
                // Try the next detector.
                continue;
            }

            // We have a non-empty role name. This is the effective role name.
            CloudRoleName = roleName;
            return;
        }

        _logger.LogWarning("Cloud role name is effectively empty. No role name detector returned a non-empty value.");
        CloudRoleName = string.Empty;
    }

    public string CloudRoleName { get; }
}