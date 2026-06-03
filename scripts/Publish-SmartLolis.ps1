param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64",
    [switch]$DeployToSteam
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $projectRoot "VPet.Plugin.SmartLolis.csproj"
$buildOutput = Join-Path $projectRoot "artifacts\bin\$Platform\$Configuration\net8.0-windows"
$localPackageRoot = Join-Path (Split-Path -Parent $projectRoot) "Smart Lolis скомпилированный"
$steamPackageRoot = "D:\Program Files (x86)\Steam\steamapps\common\VPet\mod\SmartLolis"

Write-Host "Building Smart Lolis..."
dotnet build-server shutdown | Out-Host
dotnet build $projectFile -c $Configuration -p:Platform=$Platform -p:UseSharedCompilation=false | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed."
}

function Publish-Package {
    param([string]$DestinationRoot)

    $pluginTarget = Join-Path $DestinationRoot "plugin"
    $langTarget = Join-Path $DestinationRoot "lang"

    New-Item -ItemType Directory -Path $pluginTarget -Force | Out-Null
    New-Item -ItemType Directory -Path $langTarget -Force | Out-Null

    Copy-Item (Join-Path $buildOutput "*") $pluginTarget -Force

    Copy-Item (Join-Path $projectRoot "info.lps") (Join-Path $DestinationRoot "info.lps") -Force
    Copy-Item (Join-Path $projectRoot "icon.png") (Join-Path $DestinationRoot "icon.png") -Force
    Copy-Item (Join-Path $projectRoot "lang\*") $langTarget -Recurse -Force
}

Write-Host "Publishing local package to $localPackageRoot"
Publish-Package -DestinationRoot $localPackageRoot

if ($DeployToSteam) {
    Write-Host "Publishing Steam package to $steamPackageRoot"
    Publish-Package -DestinationRoot $steamPackageRoot
}

Write-Host "Done."
