using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
/// <summary>
/// Options to for namedpipe.
/// </summary>
public class NamedPipeOptions
{
    /// <summary>
    /// Gets or sets the time span before connection established.
    /// Optional. The default value is 30 seconds.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default timeout for sending or receiving a message;
    /// Optional. Default to 2 minutes.
    /// </summary>
    public TimeSpan DefaultMessageTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
