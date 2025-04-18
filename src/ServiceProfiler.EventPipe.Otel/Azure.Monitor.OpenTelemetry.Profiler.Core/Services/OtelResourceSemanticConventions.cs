namespace Azure.Monitor.OpenTelemetry.Profiler.Core.Services;

internal static class OtelResourceSemanticConventions
{
    // Refer to: https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/Shared/ResourceSemanticConventions.cs
    public const string AttributeServiceName = "service.name";
    public const string AttributeServiceNamespace = "service.namespace";
    public const string AttributeServiceInstance = "service.instance.id";
    public const string AttributeServiceVersion = "service.version";

    public const string AttributeTelemetrySdkName = "telemetry.sdk.name";
    public const string AttributeTelemetrySdkLanguage = "telemetry.sdk.language";
    public const string AttributeTelemetrySdkVersion = "telemetry.sdk.version";
}