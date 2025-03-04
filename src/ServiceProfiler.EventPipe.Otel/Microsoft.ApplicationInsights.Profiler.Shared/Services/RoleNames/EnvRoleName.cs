using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class EnvRoleName : IRoleNameSource
{
    public EnvRoleName(string envVariableName)
    {
        if (string.IsNullOrEmpty(envVariableName))
        {
            throw new ArgumentException($"'{nameof(envVariableName)}' cannot be null or empty.", nameof(envVariableName));
        }

        CloudRoleName = Environment.GetEnvironmentVariable(envVariableName);
    }
    public string CloudRoleName { get; }
}