# Azure Monitor OpenTelemetry Profiler for .NET (PREVIEW)

## Description

Welcome to the home page of `Azure Monitor OpenTelemetry Profiler for .NET (PREVIEW)`.

## Get Started

### Step 0: Prerequisites

- **.NET 8 SDK**: Install the latest .NET Core SDK from [here](https://dotnet.microsoft.com/download/dotnet).

### Step 1: Create an ASP.NET Core Application

If you don't have an app already, you can create a new web API project using the following command:

```sh
dotnet new web
```

### Step 2: Add NuGet Package Reference

Add a reference to the latest NuGet packages:

```sh
dotnet add package Azure.Monitor.OpenTelemetry.Profiler.AspNetCore --prerelease
```

This will automatically add a dependency to `Azure.Monitor.OpenTelemetry.AspNetCore`.

### Step 3: Enable Application Insights with OpenTelemetry

Follow the [instructions](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#enable-opentelemetry-with-application-insights) to enable Azure Monitor OpenTelemetry for .NET.

Verify that the connection to Application Insights works -- [Confirm Data is Flowing](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#confirm-data-is-flowing).

### Step 4: Enable Profiler

Append the call to `UseProfiler()` in your code:

```csharp
using Azure.Monitor.OpenTelemetry.Profiler.AspNetCore;

...

builder.Services.AddOpenTelemetry()
        .UseAzureMonitor()
        .UseProfiler();  // Append this line

...
```

### Step 5: Run Your Application

Run your application and check the log output. A successful execution will look like this:

```sh
PS > dotnet run

Building...

info: Microsoft.ApplicationInsights.Profiler.Shared.Services.TraceScavenger.TraceScavengerService[0]
      TraceScavengerService started. Initial delay: 00:00:00, Grace period from last access: 00:10:00
info: Microsoft.ApplicationInsights.Profiler.Shared.Services.TraceScavenger.TraceScavengerListener[0] 
      File scavenger started.
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5143
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\
info: Azure.Monitor.OpenTelemetry.Profiler.AspNetCore.ServiceProfilerAgentBootstrap[0]
      Starting application insights profiler with connection string: InstrumentationKey=5d…
info: Azure.Monitor.OpenTelemetry.Profiler.Core.DumbTraceControl[0]
      Start writing trace file C:\Users\aaa\AppData\Local\Temp\SPTraces\...
info: Azure.Monitor.OpenTelemetry.Profiler.Core.EventListeners.TraceSessionListener[0]
      Activity detected.
info: Azure.Monitor.OpenTelemetry.Profiler.Core.DumbTraceControl[0]
      Finished writing trace file C:\Users\aaa\AppData\Local\Temp\SPTraces\b73520a6-931f-4207-b602-0d72d376609a.nettrace.
info: Azure.Monitor.OpenTelemetry.Profiler.Core.TraceUploaderProxy[0]
      Uploader to be used: C:\...\Microsoft.ApplicationInsights.Profiler.Uploader.dll

Uploader Hosting Environment is set to: Development.
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\...
info: Microsoft.ApplicationInsights.Profiler.Uploader.TraceUploaderByNamedPipe[0]
      Service Profiler trace uploaded.
info: Microsoft.ApplicationInsights.Profiler.Uploader.TraceUploaderByNamedPipe[0]
      Trace file will be preserved at: C:\...\*.etl.zip
info: Microsoft.ApplicationInsights.Profiler.Uploader.TraceUploaderByNamedPipe[0]
      Sending customer events
info: Microsoft.ApplicationInsights.Profiler.Uploader.TraceUploaderByNamedPipe[0]
      AAD auth is enabled.
info: Microsoft.Hosting.Lifetime[0]
      Application is shutting down...
info: Azure.Monitor.OpenTelemetry.Profiler.Core.TraceUploaderProxy[0] 
      Call upload trace finished. Exit code: 0
```

### Step 6: View Profiler Data

You can view the profiler data by following [these instructions](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-data).

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Data Collection

The software may collect information about you and your use of the software and send it to Microsoft. Microsoft may use this information to provide services and improve our products and services. You may turn off the telemetry as described in the repository. There are also some features in the software that may enable you and Microsoft to collect data from users of your applications. If you use these features, you must comply with applicable law, including providing appropriate notices to users of your applications together with a copy of Microsoft’s privacy statement. Our privacy statement is located at <https://go.microsoft.com/fwlink/?LinkID=824704>. You can learn more about data collection and use in the help documentation and our privacy statement. Your use of the software operates as your consent to these practices.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
