using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class EnvRoleNameDetector : IRoleNameDetector
{
    private readonly string _envVariableName;

    public EnvRoleNameDetector(string envVariableName)
    {
        if (string.IsNullOrEmpty(envVariableName))
        {
            throw new ArgumentException($"'{nameof(envVariableName)}' cannot be null or empty.", nameof(envVariableName));
        }

        _envVariableName = envVariableName;
    }

    public string? GetRoleName() => Environment.GetEnvironmentVariable(_envVariableName);
}