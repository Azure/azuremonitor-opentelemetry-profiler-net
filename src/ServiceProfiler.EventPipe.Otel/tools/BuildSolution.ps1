param(
    [ValidateSet('Debug', 'Release')]
    [Parameter(Mandatory)]
    [string]$Configuration,
    [switch]$Rebuild
)

$Undefined = "Undefined"

function GetAVersionNumber {
    param (
        [int]$Major = "99"
    )
    
    $rev = [float](Get-Date -Format "mmss");
    $rev = $rev % 65536;

    return (Get-Date -Format "$Major.yyMM.ddHH.$rev");
}


Write-Host Target Configuration: $Configuration. ReBuild: $Rebuild

IF ($Configuration -eq $Undefined) {
    Write-Host "Configuration is not set."
    Exit
}

$AssemblyVersion = GetAVersionNumber("99");
Write-Host Assembly Version Number: $AssemblyVersion

Set-Location $PSScriptRoot

$SolutionDir = Split-Path -Parent -Path ( Split-Path -Parent -Path $PSScriptRoot)
$SolutionPath = Join-Path $SolutionDir -ChildPath "Azure.Monitor.OpenTelemetry.Profiler.sln"

if ($Rebuild) {
    Write-Host "Clean the solution for rebuilding..."
    Write-Debug "dotnet clean $SolutionPath"
    dotnet clean $SolutionPath
}

dotnet restore $SolutionPath
dotnet build -c $Configuration --no-restore $SolutionPath /p:AssemblyVersion=$AssemblyVersion

Write-Host "Archive the Uploader"
$UploaderSrcFolder = Join-Path $SolutionDir "ServiceProfiler.EventPipe" "ServiceProfiler.EventPipe.Upload"
$UploaderProjectFile = Join-Path $UploaderSrcFolder "ServiceProfiler.EventPipe.Upload.csproj"
$UploaderTargetFx = "net6.0"
$UploaderPublishOutput = Join-Path $SolutionDir "ServiceProfiler.EventPipe" "ServiceProfiler.EventPipe.Upload" "bin" "$Configuration" "$UploaderTargetFx" "publish"
$UploaderArchiveDestinationDir = Join-Path $SolutionDir "ServiceProfiler.EventPipe.Otel" "Azure.Monitor.OpenTelemetry.Profiler.AspNetCore" "obj" "$Configuration" "Uploader"
$UploaderArchiveDestination = Join-Path $UploaderArchiveDestinationDir "Uploader.zip"
dotnet publish $UploaderProjectFile --no-build --nologo -f net6.0 -c $Configuration --no-restore --disable-build-servers
New-Item -ItemType Directory $UploaderArchiveDestinationDir -Force
Compress-Archive -Path ($UploaderPublishOutput + "/*") -DestinationPath $UploaderArchiveDestination -CompressionLevel Optimal -Force
Get-ChildItem $UploaderArchiveDestination
