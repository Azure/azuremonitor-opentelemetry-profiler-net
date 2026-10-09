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

    /// <summary>
    /// The budget for messages whose preparation or handling can legitimately take minutes, such as
    /// the uploader validating every sample or the profiler building the additional-data payload.
    /// </summary>
    /// <remarks>
    /// Both ends of such an exchange must use this value. The two processes do not share
    /// configuration - the uploader runs separately and only ever sees the defaults - so a reader
    /// that falls back to <see cref="DefaultMessageTimeout"/> while the sender uses this one will
    /// give up while the sender is still well within its own budget, which is what discarded
    /// otherwise viable traces in issue #192.
    /// <para>
    /// Note the two sides are not measuring the same thing even when they use the same value: the
    /// reader's budget starts when it begins waiting and so covers the peer's preparation as well
    /// as the transfer, whereas the sender's covers only the transfer. The reader therefore needs
    /// at least as much as the sender, never less.
    /// </para>
    /// </remarks>
    internal static readonly TimeSpan ExtendedMessageTimeout = TimeSpan.FromMinutes(10);
}
