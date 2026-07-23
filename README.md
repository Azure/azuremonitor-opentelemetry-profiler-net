# Azure Monitor OpenTelemetry Profiler for .NET

[![NuGet](https://img.shields.io/nuget/v/Azure.Monitor.OpenTelemetry.Profiler)](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Profiler/)

## Description

The Azure Monitor OpenTelemetry Profiler captures detailed performance traces of your live .NET applications with minimal overhead. It helps you find slow code paths, high-CPU methods, and bottlenecks, then surfaces fixes through [Code Optimizations](https://learn.microsoft.com/azure/azure-monitor/insights/code-optimizations-profiler-overview#code-optimizations) in Application Insights. The profiler runs on a schedule (random sampling) and can also trigger on high CPU or memory.

**Learn more:** [optix — Code Optimizations skills for Copilot](https://github.com/microsoft/code-optimizations-skills) · [Profiler Agent Selection Guide](./docs/ProfilerAgentSelectionGuide.md) · [CPU](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/CPU-Usage-Monitoring) & [Memory](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/Memory-Usage-Monitoring) usage monitoring

## Get Started

Using this profiler is two phases: **enable** it (once), then **analyze** the traces it collects (ongoing). Enabling takes three steps.

### Before you begin

You'll need:

- [.NET 8.0 or later](https://dotnet.microsoft.com/download/dotnet)
- An [Application Insights resource](https://learn.microsoft.com/azure/azure-monitor/app/create-workspace-resource#create-a-workspace-based-resource)
- Its connection string set as the `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable

### Step 1 — Add the profiler package

> 🚀 **On Azure App Service (Windows)?** You can skip the package reference and code change entirely — see [Codeless enablement](#codeless-enablement-no-code-change--beta) below.

```sh
dotnet add package Azure.Monitor.OpenTelemetry.Profiler --prerelease
```

### Step 2 — Enable the profiler in one line

Find the row that matches the telemetry SDK you already use, and add the highlighted call where you configure telemetry:

| Your SDK | Add this call | Detailed walkthrough |
|---|---|---|
| **Application Insights SDK for ASP.NET Core** (`Microsoft.ApplicationInsights.AspNetCore`) | `AddApplicationInsightsTelemetry().AddAzureMonitorProfiler();` | [Option A](#option-a-application-insights-sdk-for-aspnet-core-experimental) |
| **Azure Monitor OpenTelemetry distro** (`Azure.Monitor.OpenTelemetry.AspNetCore`) | `AddOpenTelemetry().UseAzureMonitor().AddAzureMonitorProfiler();` | [Option B](#option-b-azure-monitor-opentelemetry-distro) |
| **Application Insights SDK for ASP.NET Core — classic 2.x** (legacy) | Not supported — use [Application Insights Profiler for ASP.NET Core](https://github.com/microsoft/ApplicationInsights-Profiler-AspNetCore) instead | — |

### Step 3 — Run your app

```sh
dotnet run
```

Profiler traces appear in Application Insights after a few minutes. [How to view them →](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-data)

> 🤖 **Prefer zero config?** Let **optix** enable it for you. Install the [Code Optimizations skills for Copilot CLI](https://github.com/microsoft/code-optimizations-skills) and run:
> ```sh
> copilot "Help me enable the Application Insights Profiler"
> ```
> See [Enable the Profiler with optix](./docs/AddAzureMonitorProfilerWithCoPilot.md).

**Next:** once traces are flowing, [analyze them with optix](#analyze-performance-with-optix) to turn bottlenecks into concrete code fixes. Need more detail on enabling? Expand the walkthrough for your SDK below.

---

### Option A: Application Insights SDK for ASP.NET Core (Experimental)

> ⚠️ **Experimental** — This integration is under active development. Please [report any issues](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/issues/new) you encounter.

The current `Microsoft.ApplicationInsights.AspNetCore` SDK is an OpenTelemetry-based wrapper. Since it already configures OpenTelemetry internally, you can enable the profiler **without** adding `Azure.Monitor.OpenTelemetry.AspNetCore` or calling `UseAzureMonitor()`.

<details>
<summary><strong>Show setup walkthrough</strong></summary>

#### Prerequisites

In addition to the [Get Started prerequisites](#get-started): your project must reference the current OpenTelemetry-based [`Microsoft.ApplicationInsights.AspNetCore`](https://www.nuget.org/packages/Microsoft.ApplicationInsights.AspNetCore/) SDK. The legacy classic 2.x release is not supported by this profiler — use [Microsoft Application Insights Profiler for ASP.NET Core](https://github.com/microsoft/ApplicationInsights-Profiler-AspNetCore) instead.

#### Walkthrough

1. **Add NuGet Packages**

    ```sh
    dotnet add package Microsoft.ApplicationInsights.AspNetCore --version "3.*-*"
    dotnet add package Azure.Monitor.OpenTelemetry.Profiler --prerelease
    ```

    Or in your `.csproj`:

    ```xml
    <ItemGroup>
        <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="[3.*-*, 4.0.0)" />
        <PackageReference Include="Azure.Monitor.OpenTelemetry.Profiler" Version="[1.*-*, 2.0.0)" />
    </ItemGroup>
    ```

2. **Enable the Profiler**

    Chain `AddAzureMonitorProfiler()` after `AddApplicationInsightsTelemetry()`:

    ```csharp
    using Azure.Monitor.OpenTelemetry.Profiler;

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddApplicationInsightsTelemetry().AddAzureMonitorProfiler();

    var app = builder.Build();
    app.Run();
    ```

3. **Set up the connection string**

    Refer to the [connection string section](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#paste-the-connection-string-in-your-environment) for all options. For local testing in PowerShell:

    ```powershell
    $env:APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=5d..."
    ```

4. **Run & Verify**

    ```sh
    dotnet run
    ```

    Look for profiler startup log messages like:

    ```
    info: Azure.Monitor.OpenTelemetry.Profiler.ServiceProfilerAgentBootstrap[0]
        Starting application insights profiler with connection string: InstrumentationKey=5d…
    ```

    After a few minutes, traces will appear in Application Insights — see [how to view profiler data](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-data).

📖 **Full example:** [aspnetcore-aisdk3](./examples/aspnetcore-aisdk3)

</details>

---

### Option B: Azure Monitor OpenTelemetry Distro

<details>
<summary><strong>Show setup walkthrough</strong></summary>

#### Prerequisites

In addition to the [Get Started prerequisites](#get-started): this profiler works with the [Azure Monitor OpenTelemetry distro](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore).

#### Walkthrough

Assuming you are building an **ASP.NET Core application**:

1. **Create a .NET Application** (skip if you have one already)

    ```sh
    dotnet new web
    ```

2. **Add NuGet Packages**

    ```sh
    dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore --prerelease
    dotnet add package Azure.Monitor.OpenTelemetry.Profiler --prerelease
    ```

    _Tip: use [floating versions](https://learn.microsoft.com/nuget/concepts/dependency-resolution#floating-versions) to stay on the latest package:_

    ```xml
    <ItemGroup>
        <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="[1.*-*, 2.0.0)" />
        <PackageReference Include="Azure.Monitor.OpenTelemetry.Profiler" Version="[1.*-*, 2.0.0)" />
    </ItemGroup>
    ```

3. **Enable Application Insights with OpenTelemetry**

    Follow the [instructions](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#enable-opentelemetry-with-application-insights) to enable Azure Monitor OpenTelemetry for .NET, then verify that [data is flowing](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#confirm-data-is-flowing).

4. **Enable Profiler**

    Append the call to `AddAzureMonitorProfiler()` in your code:

    ```csharp
    using Azure.Monitor.OpenTelemetry.AspNetCore;
    // Import the Azure.Monitor.OpenTelemetry.Profiler namespace.
    using Azure.Monitor.OpenTelemetry.Profiler;

    ...
    builder.Services.AddOpenTelemetry()
            .UseAzureMonitor()
            .AddAzureMonitorProfiler();  // Add Azure Monitor Profiler
    ...
    ```

5. **Run Your Application**

    Run your application and verify the profiler starts. Look for this in the log output:

    ```sh
    info: Azure.Monitor.OpenTelemetry.Profiler.ServiceProfilerAgentBootstrap[0]
        Starting application insights profiler with connection string: InstrumentationKey=5d…
    info: Azure.Monitor.OpenTelemetry.Profiler.Core.DumbTraceControl[0]
        Start writing trace file C:\Users\aaa\AppData\Local\Temp\SPTraces\...
    ```

6. **View Profiler Data**

    After a few minutes, profiler traces will appear in Application Insights. Follow [these instructions](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-data) to view them.

    ![sample trace](./images/sample-trace.png)

📖 **Full example:** [aspnetcore-webapi](./examples/aspnetcore-webapi)

</details>

---

## Codeless enablement (no code change) — Beta

Running on **Azure App Service**? You can enable the profiler **without changing your app's code, adding a NuGet package, or recompiling**. A Kudu/SCM **Site Extension** (Windows) injects an ASP.NET Core `IHostingStartup` at process start, detects your app's telemetry stack (OpenTelemetry or classic Application Insights), and enables the matching profiler automatically. Enablement is **fail-safe** — if anything is incompatible, the profiler disables itself and your app keeps running.

All you need is an `APPLICATIONINSIGHTS_CONNECTION_STRING` app setting and a supported .NET telemetry stack.

- 📖 [Enable the profiler codelessly (site extension)](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/How-to-enable-profiler-codelessly)
- 🔍 [Diagnose codeless startup](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/How-to-diagnose-codeless-startup)

---

## Analyze Performance with optix

Enabling the profiler is a one-time step. The real, ongoing value is **analysis** — turning collected traces into fixes. [optix](https://github.com/microsoft/code-optimizations-skills) (Code Optimizations skills for Copilot) automates that end to end:

1. **Ingest** — finds recent profiler traces on your Application Insights resource and downloads one.
2. **Identify** — extracts the hot path and ranks the top CPU and latency contributors.
3. **Correlate** — links hot frames to a specific operation or distributed trace, plus related errors and failed dependencies.
4. **Recommend** — maps hot frames to your source code and proposes concrete changes.
5. **Verify** — re-profiles after your change to confirm the bottleneck is gone.

Install the [Code Optimizations skills for Copilot CLI](https://github.com/microsoft/code-optimizations-skills), then ask:

```sh
copilot "Analyze my Application Insights Profiler traces and show me the top CPU bottlenecks"
```

No profiler data yet? optix will guide you back to [enabling the profiler](#get-started).

---

## Next

- [Analyze performance with optix (Copilot CLI)](#analyze-performance-with-optix)
- [Enable the Profiler with optix (Copilot CLI)](./docs/AddAzureMonitorProfilerWithCoPilot.md)
- [Profiling Azure Service Bus Applications](./docs/ServiceBusSetup.md)
- [Setup the Role name](./docs/SetupCloudRoleName.md)
- [Enable the Profiler codelessly (site extension)](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/How-to-enable-profiler-codelessly)
- [Configuration Guide](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/Configurations)
- [CPU Usage Monitoring](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/CPU-Usage-Monitoring)
- [Memory Usage Monitoring](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/Memory-Usage-Monitoring)
- [Read the examples](#examples)

## Troubleshooting

If profiles are not appearing in Application Insights:

1. **Check logs** — Enable debug logging to see the profiler pipeline activity:
    ```json
    {
      "Logging": {
        "LogLevel": {
          "Microsoft.ServiceProfiler": "Debug",
          "Microsoft.ApplicationInsights.Profiler": "Debug"
        }
      }
    }
    ```
2. **Verify connection string** — Ensure your Application Insights connection string is configured correctly.
3. **Check triggers** — If using CPU or memory triggers, verify your thresholds are below observed usage levels. See [CPU Usage Monitoring](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/CPU-Usage-Monitoring) and [Memory Usage Monitoring](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/wiki/Memory-Usage-Monitoring).
4. **Entra authentication** — If your Application Insights resource requires Entra (AAD) authentication, configure credentials accordingly. The profiler will log an error if authentication is missing.

If you're still experiencing issues, please [open an issue](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/issues/new) with your environment details and relevant log output.

## Examples

Learn more by following the examples:

- [Application Insights SDK for ASP.NET Core + Profiler](./examples/aspnetcore-aisdk3) — for [Option A](#option-a-application-insights-sdk-for-aspnet-core-experimental)
- [Azure Monitor OpenTelemetry Distro + Profiler (ASP.NET Core WebAPI)](./examples/aspnetcore-webapi) — for [Option B](#option-b-azure-monitor-opentelemetry-distro)

## Build Status

| Pipeline | Status |
| ----------- | ----------- |
| Package | [![Nuget](https://img.shields.io/nuget/v/Azure.Monitor.OpenTelemetry.Profiler)](https://www.nuget.org/packages/Azure.Monitor.OpenTelemetry.Profiler/) |
| PR Build | [![Build Status](https://dev.azure.com/devdiv/OnlineServices/_apis/build/status%2FOneBranch%2FServiceProfiler%2FBuilds%2FEP-OTel-Profiler-PR?repoName=ServiceProfiler-EP-Profiler)](https://dev.azure.com/devdiv/OnlineServices/_build/latest?definitionId=25440&repoName=ServiceProfiler-EP-Profiler) |
| Official Build | [![Build Status](https://dev.azure.com/devdiv/OnlineServices/_apis/build/status%2FOneBranch%2FServiceProfiler%2FBuilds%2FEP-OTel-Profiler-Official?repoName=ServiceProfiler-EP-Profiler&branchName=main)](https://dev.azure.com/devdiv/OnlineServices/_build/latest?definitionId=25454&repoName=ServiceProfiler-EP-Profiler&branchName=main) |

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
