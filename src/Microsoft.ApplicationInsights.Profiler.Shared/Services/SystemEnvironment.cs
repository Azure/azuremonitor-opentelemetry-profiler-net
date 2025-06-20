using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

/// <summary>
/// An adapter for <see cref="System.Environment" /> methods to be mockable.
/// </summary>
internal class SystemEnvironment : IEnvironment
{
    /// <inheritdoc />
    public string ExpandEnvironmentVariables(string name) => Environment.ExpandEnvironmentVariables(name);
}
