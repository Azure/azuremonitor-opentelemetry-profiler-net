namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

/// <summary>
/// Trace upload mode.
/// </summary>
public enum UploadMode
{
    /// <summary>Do not upload trace.</summary>
    Never = -1,
    /// <summary>Upload trace when profiling succeeded.</summary>
    OnSuccess = 0,
    /// <summary>Always upload trace. This might help with troubleshooting.</summay>
    Always = 1,
}
