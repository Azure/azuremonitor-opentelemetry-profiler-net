using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.Services;

internal class OtelResourceRoleNameDetector : IRoleNameDetector
{
    private readonly OtelResourceDetector _otelResourceDetector;

    public OtelResourceRoleNameDetector(OtelResourceDetector otelResourceDetector)
    {
        _otelResourceDetector = otelResourceDetector ?? throw new ArgumentNullException(nameof(otelResourceDetector));
    }

    public string? GetRoleName() => Normalize(_otelResourceDetector.GetResource(OtelResourceSemanticConventions.AttributeServiceName));

    private static string? Normalize(string? serviceName)
    {
        if (string.IsNullOrEmpty(serviceName))
        {
            return null;
        }

        // When service.name is not set, it is set to "unknown_service:processName".
        // Refer to: https://github.com/open-telemetry/opentelemetry-dotnet/blob/1edcddbe0091b452dfb6a46fa34ff7b7c1374d3e/src/OpenTelemetry/Resources/ResourceBuilder.cs#L26C1-L27C1
        // That is implementation details and could change in the future. Check back on main branch for updates.
        // https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry/Resources/ResourceBuilder.cs
        if (serviceName.StartsWith("unknown_service", StringComparison.Ordinal))
        {
            return null;
        }

        return serviceName;
    }
}