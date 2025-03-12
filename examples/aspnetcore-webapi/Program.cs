using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Profiler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()          // Enable Azure Monitor OpenTelemetry distro for ASP.NET Core
    .AddAzureMonitorProfiler(); // Add Azure Monitor Profiler

var app = builder.Build();

app.MapGet("/", async () =>
{
    await Task.Delay(2000);
    return "Hello World!";
});

app.Run();
