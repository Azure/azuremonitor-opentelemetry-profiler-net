# Profiling Agent Selection Guide

Welcome to the Profiling Agent Selection Guide! Profiling is an essential step in optimizing your application’s performance. This document will help you choose the best profiler agent based on your needs, environment, and specific use case.

There are 2 major profiling technologies we supported: **ETW** and **EventPipe**. ETW works only on Windows. EventPipe works wherever `.NET runtime` exists.

A rule of thumb is that if you are targeting [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework), use **ETW** profiler. If you are targeting [.NET](https://dotnet.microsoft.com/download/dotnet), use **EventPipe**.

Of course, it's not one size fit for all. There are more to consider, please find more details below.

## ETW

If you are targeting [.NET Framework](https://dotnet.microsoft.com/download/dotnet-framework), you will have to use ETW profiler. It can be turned on for the following Azure services. Click on the links to learn more about turning it on:

- [Azure App Service](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler)
- [Azure Function](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-azure-functions)
- [Azure Cloud Service](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-cloudservice)
- [Azure Service Fabric Apps](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-servicefabric)
- [Azure Virtual Machines and Azure Virtual Machine Scale Set](https://learn.microsoft.com/azure/azure-monitor/profiler/profiler-vm)

There's no code change needed for your application. The profiler runs out of proc, and the trace file is heavier.

## EventPipe

If you are targeting [.NET](https://dotnet.microsoft.com/download/dotnet), **.NET 8** for example, you can still choose to use **ETW profiler** for the environments listed above, or you can choose to use **EventPipe Profiler** too.

_⚠️ Tip: Do NOT use both profiler at the same time considering the overhead._

If you are planning to host your applications in Linux, or run your application inside containers, use **EventPipe Profiler**. Some well known host platforms like:

- [Azure Kubernetes Services](https://learn.microsoft.com/azure/aks/)
- [Azure Container Apps](https://azure.microsoft.com/products/container-apps)
- [Azure Container Instances](https://azure.microsoft.com/products/container-instances)

There are 2 flavors of the **EventPipe profilers**, it depends on the Application Insights SDKs you pick.

- If you are using [Azure Monitor OpenTelemetry distribution](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore), follow the instructions to use **[Azure Monitor OpenTelemetry Profiler](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net)**.

- If you are using the traditional [Application Insights for ASP.NET Core applications](https://learn.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core), follow the instructions to use [Application Insights Profiler for ASP.NET Core](https://github.com/microsoft/ApplicationInsights-Profiler-AspNetCore)

You will need to add reference to NuGet packages and redeploy your application for those Profiler Agents to work. These profiler agents runs in-proc with your application, and are lightweight.

## Get more support

Feel free to file an [issue](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/issues) if you still have questions on which profiler agent to pick.
