param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$PackageType = "private",
    [switch]$Rebuild,
    [switch]$PushNuGet,
    [string]$VersionSuffix
)

function GetNuGetPackageFileName {
    param (
        [string]$SearchRoot
    )
    return (Get-ChildItem -Path ($SearchRoot + "\*.nupkg") -Force -Recurse -File | Select-Object -Last 1).Name
}


function GenerateVersionSuffix {
    param(
        [Parameter(Mandatory)]
        [ValidateSet('Debug', 'Release')]
        [string]$Configuration
    )

    Write-Host "Generating default version suffix."
    $VersionSuffixAddon = Get-Date -Format "yyMMddHHmm"
    $VersionSuffix = "$PackageType$VersionSuffixAddon"

    if ($Configuration -eq 'Debug') {
        $VersionSuffix = $VersionSuffix + "debug"
    }

    return $VersionSuffix
}

if ($PSVersionTable.PSVersion.Major -ge 7) {
    Write-Host "PowerShell 7 or later is installed."
    Write-Host "Version: $($PSVersionTable.PSVersion)"
}
else {
    Write-Host "PowerShell 7 is not installed."
    Exit
}

Write-Host "Target Configuration: $Configuration. ReBuild: $Rebuild. Package Type: $PackageType."

$UseDefaultVersionSuffix = [string]::IsNullOrEmpty($VersionSuffix)
if ($UseDefaultVersionSuffix) {
    $VersionSuffix = [string](GenerateVersionSuffix -Configuration $Configuration)
}

Write-Host "Effective version suffix: $VersionSuffix"

$BaseDir = Split-Path -Parent $PSScriptRoot
$SolutionDir = Split-Path -Parent $BaseDir
$OutputDir = Join-Path -Path (Split-Path -Parent $SolutionDir) -ChildPath "Out"
$NuGetOutDir = Join-Path -Path $OutputDir -ChildPath "NuGets"

Write-Host "Prepare Output Folder: $OutputDir"

New-Item -ItemType Directory -Path $OutputDir -Force
New-Item -ItemType Directory -Path $NuGetOutDir -Force

$CorePackageOutputDir = Join-Path "$BaseDir" "Azure.Monitor.OpenTelemetry.Profiler.Core" "bin" $Configuration
$AspNetCorePackageOutputDir = Join-Path "$BaseDir" "Azure.Monitor.OpenTelemetry.Profiler.AspNetCore" "bin" $Configuration

Remove-Item (Join-Path $CorePackageOutputDir *.nupkg) -Force
Remove-Item (Join-Path $AspNetCorePackageOutputDir *.nupkg) -Force

Write-Host Build the solution
& $PSScriptRoot\BuildSolution.ps1 $Configuration -Rebuild:$Rebuild

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed building the solution."
    EXIT -100
}

Write-Host "Pack nuget packages"
dotnet pack (Join-Path $BaseDir "Azure.Monitor.OpenTelemetry.Profiler.Core") --no-build --no-restore --version-suffix $VersionSuffix -c $Configuration
dotnet pack (Join-Path $BaseDir "Azure.Monitor.OpenTelemetry.Profiler.AspNetCore") --no-build --no-restore --version-suffix $VersionSuffix -c $Configuration

$NuGetCoreFileName = GetNuGetPackageFileName -SearchRoot "$CorePackageOutputDir"
$NuGetAspNetCoreFileName = GetNuGetPackageFileName -SearchRoot "$AspNetCorePackageOutputDir"

XCOPY (Join-Path $CorePackageOutputDir $NuGetCoreFileName) $NuGetOutDir /y /f
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed copying the NuGet package file"
    EXIT -200
}

XCOPY (Join-Path $AspNetCorePackageOutputDir $NuGetAspNetCoreFileName) $NuGetOutDir /y /f
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed copying the NuGet package file"
    EXIT -200
}

Write-Host "Package succeeded :-)"

if ($PushNuGet) {
    $DDFiles = "\\ddfiles\Team\Public"
    if (-not (Test-Path -Path $DDFiles)) {
        Write-Error "NuGet feed base doesn't exist at: $DDFiles. Are you connected with VPN?"
        EXIT
    }
    Write-Host "NuGet feed base exists: $DDFiles"

    $NuGetFeedPath = Join-Path $DDFiles "DiagnosticServices" "NuGets"
    New-Item -ItemType Directory -Path $NuGetFeedPath -Force

    Push-Location $PSScriptRoot
    Write-Host "Push NuGet package" (Join-Path $NuGetOutDir $NuGetCoreFileName)
    # It looks like pushing to feed from localbox is disabled
    # dotnet nuget push (Join-Path $NuGetOutDir $NuGetCoreFileName) -s https://pkgs.dev.azure.com/devdiv/_packaging/DiagnosticServices/nuget/v3/index.json
    XCOPY (Join-Path $NuGetOutDir $NuGetCoreFileName) $NuGetFeedPath /y /f
    Write-Host "Push NuGet package" (Join-Path $NuGetOutDir $NuGetAspNetCoreFileName)
    # dotnet nuget push (Join-Path $NuGetOutDir $NuGetAspNetCoreFileName) -s https://pkgs.dev.azure.com/devdiv/_packaging/DiagnosticServices/nuget/v3/index.json
    XCOPY (Join-Path $NuGetOutDir $NuGetAspNetCoreFileName) $NuGetFeedPath /y /f
    
    Write-Host Tagging with $VersionSuffix
    git tag $VersionSuffix
    Write-Host Push the tags by: git push --tags
    Pop-Location
}
