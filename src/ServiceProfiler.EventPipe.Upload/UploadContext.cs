//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using CommandLine;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ServiceProfiler.Contract.Agent;
using System;

namespace Microsoft.ApplicationInsights.Profiler.Core.Contracts
{
    internal class UploadContext
    {
        private const char InstrumentationKeyShortKeyName = 'i';
        private const string InstrumentationKeyKeyName = "iKey";
        private const string EndpointKeyName = "host";
        private const string SessionIdKeyName = "sessionId";
        private const string StampIdKeyName = "stampId";
        private const char StampIdShortKeyName = 's';
        private const char TraceFilePathShortKeyName = 't';
        private const string TraceFilePathKeyName = "trace";
        private const string MetadataFilePathKeyName = "metadata";
        private const string PreserveTraceFileKeyName = "preserve";
        private const string SkipEndpointCertificateValidationKeyName = "insecure";
        private const string UploadModeKeyName = "uploadMode";
        private const string SampleActivityFilePathKeyName = "sampleActivityFilePath";
        private const string PipeNameKeyName = "pipeName";
        private const char PipeNameShortKeyName = 'p';
        private const char RoleNameShortKeyName = 'r';
        private const string RoleNameKeyName = "roleName";
        private const char TriggerTypeShortKeyName = 'u';
        private const string TriggerTypeKeyName = "trigger";

        private const char EnvironmentShortKeyName = 'e';
        private const string EnvironmentKeyName = "environment";

        private const char TraceFileFormatShortKeyName = 'f';
        private const string TraceFileFormatKeyName = "traceFileFormat";

        [Option(InstrumentationKeyShortKeyName, InstrumentationKeyKeyName, Required = true, HelpText = "Application Insights instrumentation key.")]
        public Guid AIInstrumentationKey { get; set; }

        [Option(EndpointKeyName, Required = true, HelpText = "Microsoft Application Insights Profiler endpoint.")]
        public Uri HostUrl { get; set; } = null!;

        [Option(SessionIdKeyName, Required = true, HelpText = "Trace session id.")]
        public DateTimeOffset SessionId { get; set; }

        [Option(StampIdShortKeyName, StampIdKeyName, Required = true, HelpText = "StampId used to upload the trace file.")]
        public string StampId { get; set; } = null!;

        [Option(TraceFilePathShortKeyName, TraceFilePathKeyName, Required = true, HelpText = "File path to trace file.")]
        public string TraceFilePath { get; set; } = null!;

        [Option(MetadataFilePathKeyName, Required = false, HelpText = "File path to the metadata file.")]
        public string MetadataFilePath { get; set; } = null!;

        [Option(PreserveTraceFileKeyName, Required = false, Default = false, HelpText = "Preserve the trace file locally.")]
        public bool PreserveTraceFile { get; set; }

        [Option(SkipEndpointCertificateValidationKeyName, Required = false, Default = false, HelpText = "Allow insecure profile endpoint connections when using SSL.")]
        public bool SkipEndpointCertificateValidation { get; set; }

        [Option(UploadModeKeyName, Required = false, Default = UploadMode.OnSuccess, HelpText = "The mode for uploading behavior. Valid values are: Never, OnSuccess and Always.")]
        public UploadMode UploadMode { get; set; }

        [Option(SampleActivityFilePathKeyName, Required = true, HelpText = "Path to the file which contains all serialized activity samples.")]
        public string SerializedSampleFilePath { get; set; } = null!;

        [Option(PipeNameShortKeyName, PipeNameKeyName, Required = false, HelpText = "Named pipe name for client and uploader communication. NamedPipe is preferred when the value is provided.")]
        public string? PipeName { get; set; }

        [Option(RoleNameShortKeyName, RoleNameKeyName, Required = false, HelpText = "Cloud role name of the customer app recorded in the telemetry.")]
        public string? RoleName { get; set; }

        [Option(EnvironmentShortKeyName, EnvironmentKeyName, Required = false, HelpText = "Environment for Uploader.", Default = "Production")]
        public string? Environment { get; set; }

        [Option(TriggerTypeShortKeyName, TriggerTypeKeyName, Required = false, HelpText = "Type of trigger that caused the collection of the trace.")]
        public string? TriggerType { get; set; }

        [Option(TraceFileFormatShortKeyName, TraceFileFormatKeyName, Required = true, HelpText = "The trace file format.", Default = ServiceProfiler.Contract.Agent.Profiler.TraceFileFormat.Netperf)]
        public string TraceFileFormat { get; set; } = ServiceProfiler.Contract.Agent.Profiler.TraceFileFormat.Netperf;

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
                argumentLine += $@" --{EnvironmentKeyName} ""{Environment}"" ";
            }

            argumentLine += $@" --{TraceFileFormatKeyName} ""{TraceFileFormat}""";

            return argumentLine;
        }

        public bool UseNamedPipe => !string.IsNullOrEmpty(PipeName);
    }
}
