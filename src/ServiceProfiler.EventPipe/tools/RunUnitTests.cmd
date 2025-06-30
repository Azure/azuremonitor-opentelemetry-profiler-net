@ECHO OFF
dotnet test %~dp0/../../../Tests/ServiceProfiler.EventPipe.Upload.Tests/ServiceProfiler.EventPipe.Upload.Tests.csproj
dotnet test %~dp0/../../../Tests/ServiceProfiler.EventPipe.Client.Tests/ServiceProfiler.EventPipe.Client.Tests.csproj
