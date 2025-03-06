# Enable Azure Monitor Profiler for an ASP.NET Core WebAPI

This is an example app to enable Azure Monitor Profiler in ASP.NET Core WebAPI with [Azure Monitor OpenTelemetry distro](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore).

Here are the steps to build this example step to step.

## Create an application

Run this command to create a web api application:

```shell
dotnet new web -n SimpleApp -o . -f net8.0
```

## Add reference to NuGet packages

Add the following 2 packages, refer to [SimpleApp.csproj](./SimpleApp.csproj) for details:

```xml
<ItemGroup>
    <PackageReference Include="Azure.Monitor.OpenTelemetry.AspNetCore" Version="[1.*-*, 2.0.0)" />
    <PackageReference Include="Azure.Monitor.OpenTelemetry.Profiler" Version="[1.*-*, 2.0.0)" />
</ItemGroup>
```

Restore the packages:

```shell
> dotnet restore SimpleApp.csproj
```

Optionally, check the resolved package version

```shell
> dotnet list package
Project 'SimpleApp' has the following package references
   [net8.0]:
   Top-level Package                                Requested        Resolved
   > Azure.Monitor.OpenTelemetry.AspNetCore         [1.*-*, 2.0.0)   1.3.0-beta.2
   > Azure.Monitor.OpenTelemetry.Profiler           [1.*-*, 2.0.0)   1.0.0-beta1
   > Microsoft.VisualStudioEng.MicroBuild.Core      1.0.0            1.0.0
```

## Update the code to enable the profiler

Refer to [Program.cs](./Program.cs) for more details:

```csharp
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()          // Enable Azure Monitor OpenTelemetry distro for ASP.NET Core
    .AddAzureMonitorProfiler(); // Add Azure Monitor Profiler
```

If you run the code now, an exception will throw:

```shell
Unhandled exception. System.InvalidOperationException: A connection string was not found. Please set your connection string.
```

## Set up the connection string

Refer to [connection string section](https://learn.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore#paste-the-connection-string-in-your-environment) to see options.
Here's a quick way for local test using environment variable in PowerShell:

```powershell
$env:APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=5d..."
```

Tips: Copy the connection string from the `overview` blade of the application insights resource.

## Bake in some delay for your operation, for example

```csharp
app.MapGet("/", async () =>
{
    await Task.Delay(2000); // 2 seconds delay
    return "Hello World!";
});
```

⚠️ Trace analysis might fail if the operation is too fast, like finished within 10 milliseconds.

## Run your application

```shell
dotnet run
```

Generate some traffic for profiler to capture the issue:

```shell
curl http://localhost:5082
```
