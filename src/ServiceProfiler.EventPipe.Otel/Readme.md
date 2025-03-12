# Azure Monitor OpenTelemetry Profiler for .NET

To enable Azure Monitor Profiler in your ASP.NET Core application using OpenTelemetry, follow these simple steps:

## Install the Required NuGet Packages

Run these commands in your terminal to add the necessary packages:

```shell
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore --prerelease
dotnet add package Azure.Monitor.OpenTelemetry.Profiler --prerelease
```

## Add Profiler to Your Application

In your `Program.cs` or `Startup.cs`, add the following code to integrate the profiler:

```csharp
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Profiler;

...
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()
    .AddAzureMonitorProfiler();  // Add AzureMonitor Profiler.
...
```

## Learn More

For more information, visit our [GitHub Repository](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net).
