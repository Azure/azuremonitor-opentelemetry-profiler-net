using System;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services;

internal class EnvRoleName : IRoleNameSource
{
    public string CloudRoleName => Environment.GetEnvironmentVariable("RoleName");
}