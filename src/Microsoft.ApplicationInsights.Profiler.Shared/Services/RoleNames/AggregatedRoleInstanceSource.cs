using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class AggregatedRoleInstanceSource : IRoleInstanceSource
{
    private readonly ILogger _logger;

    public AggregatedRoleInstanceSource(
        IEnumerable<IRoleInstanceDetector> roleInstanceDetectors,
        ILogger<AggregatedRoleInstanceSource> logger
        )
    {
        _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));

        if(!roleInstanceDetectors.Any())
        {
            _logger.LogWarning("There's no role instance detector registered. The role instance will be empty. This should not happen.");
        }

        foreach (IRoleInstanceDetector roleInstanceDetector in roleInstanceDetectors)
        {
            string roleInstance = roleInstanceDetector.GetRoleInstance()?? string.Empty;
            _logger.LogDebug("Role instance detector {detector} returned role instance: {roleInstance}", roleInstanceDetector.GetType().Name, roleInstance);
            
            if (string.IsNullOrEmpty(roleInstance))
            {
                // Try the next detector.
                continue;
            }

            // We have a non-empty role instance. This is the effective role instance.
            CloudRoleInstance = roleInstance;
            return;
        }

        _logger.LogWarning("Cloud role instance is effectively empty. No role instance detector returned a non-empty value.");
        CloudRoleInstance = string.Empty;
    }

    public string CloudRoleInstance { get; }
}