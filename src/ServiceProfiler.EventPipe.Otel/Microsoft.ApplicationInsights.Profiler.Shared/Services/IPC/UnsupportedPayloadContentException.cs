namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;

/// <summary>
/// Exception throws when the payload content can't be transmitted.
/// </summary>
public class UnsupportedPayloadContentException : System.Exception
{
    public UnsupportedPayloadContentException() { }
    public UnsupportedPayloadContentException(string message) : base(message) { }
    public UnsupportedPayloadContentException(string message, System.Exception inner) : base(message, inner) { }
}
