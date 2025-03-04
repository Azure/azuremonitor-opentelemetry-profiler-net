using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class AggregatedRoleNameSource : IRoleNameSource
{
    private List<IRoleNameSource> _roleNameSources;

    public AggregatedRoleNameSource()
    {
        // Order matters: External source provider, then known environment.
        // The first one that is not null or empty will be used as the effective role name.
        _roleNameSources = [
            new EnvRoleName("WEBSITE_SITE_NAME"), // Antares
            new EnvRoleName("RoleName"),    // Direct assignment
        ];

        string? roleName = _roleNameSources.FirstOrDefault(item => !string.IsNullOrEmpty(item.CloudRoleName))?.CloudRoleName;
        CloudRoleName = roleName ?? string.Empty;
    }

    public string CloudRoleName { get; }
}