using System;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

/// <summary>
/// Options for <see cref="TraceScavengerService"/>.
/// </summary>
public class TraceScavengerServiceOptions
{
    /// <summary>
    /// Gets or sets the initial delay before the <see cref="TraceScavengerService"> runs.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the interval for each scan.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the grace period. Do not delete the file which is accessed within the grace period.
    /// </summary>
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromMinutes(10);
}

