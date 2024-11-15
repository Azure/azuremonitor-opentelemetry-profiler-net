using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.IPC;

public class UnsupportedPayloadTypeException : Exception
{
    public UnsupportedPayloadTypeException() { }
    public UnsupportedPayloadTypeException(string message) : base(message) { }
    public UnsupportedPayloadTypeException(string message, Exception inner) : base(message, inner) { }
}