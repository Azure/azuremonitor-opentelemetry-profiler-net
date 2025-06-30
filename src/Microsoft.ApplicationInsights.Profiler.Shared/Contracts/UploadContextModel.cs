using System;
using Microsoft.ServiceProfiler.Contract.Agent;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

internal class UploadContextModel
{
    private const char InstrumentationKeyShortKeyName = 'i';
    private const string EndpointKeyName = "host";
    private const string SessionIdKeyName = "sessionId";
    private const char StampIdShortKeyName = 's';
    private const char TraceFilePathShortKeyName = 't';
    private const string MetadataFilePathKeyName = "metadata";
    private const string PreserveTraceFileKeyName = "preserve";
    private const string SkipEndpointCertificateValidationKeyName = "insecure";
    private const string UploadModeKeyName = "uploadMode";
    private const string SampleActivityFilePathKeyName = "sampleActivityFilePath";
    private const string PipeNameKeyName = "pipeName";
    private const string RoleNameKeyName = "roleName";
    private const string TriggerTypeKeyName = "trigger";
    private const string EnvironmentKeyName = "environment";
    private const string TraceFileFormatKeyName = "traceFileFormat";


    public Guid AIInstrumentationKey { get; init; }

    public Uri HostUrl { get; init; } = null!;

    public DateTimeOffset SessionId { get; init; }

    public string? StampId { get; init; }

    public string TraceFilePath { get; init; } = null!;

    public string? MetadataFilePath { get; init; }

    public bool PreserveTraceFile { get; init; } = false;

    public bool SkipEndpointCertificateValidation { get; init; } = false;

    public UploadMode UploadMode { get; init; } = UploadMode.OnSuccess;

    public string? SerializedSampleFilePath { get; init; }

    public string? PipeName { get; init; }

    public string? RoleName { get; init; }

    public string Environment { get; init; } = "Production";

    public string? TriggerType { get; init; }

    public string TraceFileFormat { get; set; } = null!;

    public override string ToString()
    {
        // TODO: Treat this as a method to serialize the parameters before passing it on to the uploader.
        // The issue is: The uploader uses CommandOptions to sort of deserializing the parameters.
        // This is somewhat awkward because of the mismatch between the serializer and the deserializer.
        string argumentLine = $@"-{TraceFilePathShortKeyName} ""{TraceFilePath}"" -{InstrumentationKeyShortKeyName} {AIInstrumentationKey} --{SessionIdKeyName} ""{TimestampContract.TimestampToString(SessionId)}"" -{StampIdShortKeyName} ""{StampId}"" --{EndpointKeyName} {HostUrl} --{MetadataFilePathKeyName} ""{MetadataFilePath}"" --{UploadModeKeyName} ""{UploadMode}"" --{SampleActivityFilePathKeyName} ""{SerializedSampleFilePath}""";

        if (!string.IsNullOrEmpty(PipeName))
        {
            argumentLine += $@" --{PipeNameKeyName} ""{PipeName}""";
        }

        if (PreserveTraceFile)
        {
            argumentLine += $" --{PreserveTraceFileKeyName}";
        }

        if (SkipEndpointCertificateValidation)
        {
            argumentLine += $" --{SkipEndpointCertificateValidationKeyName}";
        }

        if (!string.IsNullOrEmpty(RoleName))
        {
            argumentLine += $@" --{RoleNameKeyName} ""{RoleName}""";
        }

        if (!string.IsNullOrEmpty(TriggerType))
        {
            argumentLine += $@" --{TriggerTypeKeyName} ""{TriggerType}""";
        }

        if (!string.IsNullOrEmpty(Environment))
        {
            argumentLine += $@" --{EnvironmentKeyName} ""{Environment}""";
        }

        argumentLine += $@" --{TraceFileFormatKeyName} ""{TraceFileFormat}""";

        return argumentLine;
    }
}