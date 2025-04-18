using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Azure.Monitor.OpenTelemetry.Profiler.Core.Services;

/// <summary>
/// Detects the role instance from the OpenTelemetry resource attributes.
/// Notice that this feature is not stable yet in OpenTelemetry, so it may change in the future.
/// See https://opentelemetry.io/docs/specs/semconv/resource/#service for more details.
/// </summary>
internal class OtelResourceRoleInstanceDetector : IRoleInstanceDetector
{
    private readonly OtelResourceDetector _otelResourceDetector;

    public OtelResourceRoleInstanceDetector(OtelResourceDetector otelResourceDetector)
    {
        _otelResourceDetector = otelResourceDetector ?? throw new ArgumentNullException(nameof(otelResourceDetector));
    }

    public string? GetRoleInstance() => _otelResourceDetector.GetResource(OtelResourceSemanticConventions.AttributeServiceInstance);
}