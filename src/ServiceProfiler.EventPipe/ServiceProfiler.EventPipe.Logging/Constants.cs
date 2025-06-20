//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Core.Logging
{
    internal static class Constants
    {
        /// <summary>
        /// The AppID of the user's application.
        /// This is mapped to TelemetryClient.Context.User.AuthenticatedUserId (user_AuthenticatedId)
        /// </summary>
        public const string AuthenticatedUserId = nameof(AuthenticatedUserId);

        /// <summary>
        /// Mapped to TelemetryClient.Context.Component.Version (application_Version)
        /// </summary>
        public const string ComponentVersion = nameof(ComponentVersion);

        /// <summary>
        /// Mapped to TelemetryClient.Context.GetInternalContext().SdkVersion (sdkVersion)
        /// </summary>
        public const string SdkVersion = nameof(SdkVersion);

        /// <summary>
        /// An identifier of a running session.
        /// This normally is GUID generated when the process started for indicating the process lifetime.
        /// Mapped to TelemetryClient.Context.Session.Id (session_Id)
        /// </summary>
        public const string SessionId = nameof(SessionId);

        /// <summary>
        /// Machine name is PII data, we need to hash it.
        /// Mapped to TelemetryClient.Context.Cloud.RoleInstance (cloud_RoleInstance)
        /// </summary>
        public const string CloudRoleInstance = nameof(CloudRoleInstance);

        /// <summary>
        /// Operating System name
        /// Mapped to TelemetryClient.Context.Device.OperatingSystem (client_OS)
        /// </summary>
        public const string OS = nameof(OS);

        /// <summary>
        /// .NET Runtime Framework description
        /// </summary>
        public const string Runtime = nameof(Runtime);

        public const string ProcessWorkingSetInMB = nameof(ProcessWorkingSetInMB);

        public const string ProcessPagedMemoryInMB = nameof(ProcessPagedMemoryInMB);

        public const string StartTimeUTC = nameof(StartTimeUTC);

        public const string Heartbeat = nameof(Heartbeat);

        public const string DurationInSeconds = nameof(DurationInSeconds);

        public const string RunningMode = nameof(RunningMode);

        public const string EventName = nameof(EventName);

        public const string ProcessId = nameof(ProcessId);

        public const string EnablingExtensionVersion = nameof(EnablingExtensionVersion);

        public const string FeatureVersion = nameof(FeatureVersion);

        public const string FunctionHostVersion = nameof(FunctionHostVersion);

        public const string InContainer = nameof(InContainer);
    }
}
