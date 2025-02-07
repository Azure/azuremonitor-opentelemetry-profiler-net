# Use Profiler Assemblies Directly

## Overview

If you prefer not to use the GitHub package, you can configure your project to reference the assemblies and uploader directly. To do this, download and set up the necessary files, update the `.csproj` file, and restore the required NuGet packages.

## Prepare the Files

1. Download the `Lib.zip` and `TraceUpload.zip` files from the [releases](https://github.com/Azure/azuremonitor-opentelemetry-profiler-net/releases).
1. Unzip only the `Lib.zip` file. Copy the folder to the project directory where the `.csproj` file is located.
1. Create a folder named `ServiceProfiler` in the project directory and place the `TraceUpload.zip` file inside this folder.

## Update the .csproj File

1. Add the following lines to reference the assemblies:

    ```xml
    <ItemGroup>
        <Reference Include="Lib/*.dll" />
    </ItemGroup>
    ```

1. Add the following lines to ensure the `TraceUploader.zip` file is copied to the output directory:

    ```xml
    <ItemGroup>
        <None Update="ServiceProfiler/TraceUpload.zip">
            <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </None>
    </ItemGroup>
    ```

## Restore Nuget Packages

To install the necessary packages, run the following commands:

```sh
dotnet add package Azure.Core
dotnet add package Azure.Monitor.OpenTelemetry.AspNetCore --version 1.3.0-beta.2
dotnet add package CommandLineParser
dotnet add package Microsoft.AspNetCore.OpenApi
dotnet add package Microsoft.Diagnostics.NETCore.Client
dotnet add package Microsoft.Extensions.Configuration.Abstractions
dotnet add package Microsoft.Extensions.Configuration.Binder
dotnet add package Microsoft.Extensions.Hosting.Abstractions
dotnet add package Microsoft.Extensions.Logging.Abstractions
dotnet add package Microsoft.Extensions.Options
dotnet add package OpenTelemetry 
dotnet add package Swashbuckle.AspNetCore 
dotnet add package System.Collections.Immutable
dotnet add package System.Text.Json 
```
