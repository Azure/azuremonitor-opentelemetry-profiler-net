# [Deprecated] Restore NuGet Packages from Private Feed

This is for EARLY ACCESS only and is deprecated. Please follow the [Readme](../README.md) and file issues when there are questions.

## Overview

You can restore the NuGet packages from a temporary private feed by following the steps below.

### 1. Add NuGet Source

Create a `nuget.config` file in your project root with the following content.

```xml
<configuration>
    <packageSources>
        <!-- To inherit the global NuGet package sources, remove the <clear/> line below -->
        <clear />
        <add key="nuget" value="https://api.nuget.org/v3/index.json" />
        <!-- Add the following line -->
        <add key="otel-early-access" value="http://172.179.151.190:5000/v3/index.json" /> 
    </packageSources>
</configuration>
```

Alternatively, you can add the source using the .NET CLI:

```sh
dotnet nuget add source http://172.179.151.190:5000/v3/index.json
```

### 2. Add NuGet Package

To add the `Azure.Monitor.OpenTelemetry.Profiler` package, run the following command:

```sh
dotnet add package Azure.Monitor.OpenTelemetry.Profiler --prerelease
```
