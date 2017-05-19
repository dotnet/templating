#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Runtime="win7-x86",
    [bool]$PerformFullFrameworkBuild=$true,
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-Help]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>                Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -PerformFullFrameworkBuild <$true|$false>     Whether or not to build for NET45 as well as .NET Core"
    Write-Host "  -Help                                         Display this help message"
    exit 0
}

if (!$PerformFullFrameworkBuild)
{
    Write-Host "Skipping NET45 build..."
}
else
{
    Write-Host "NET45 build is enabled, if invoking from setup, set the environment variable DN3FFB to $false to prevent this..."
}

$RepoRoot = "$PSScriptRoot"
$ArtifactsDir = "$RepoRoot\artifacts"
$DevDir = "$RepoRoot\dev"
$env:CONFIGURATION = $Configuration;

rm "$DevDir" -Force -Recurse
if(Test-Path "$DevDir") { throw "Failed to remove 'dev'" }

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$RepoRoot\.dotnet\"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

if (Test-Path "$RepoRoot\artifacts")
{
    rm "$RepoRoot\artifacts" -Force -Recurse
}

mkdir "$RepoRoot\artifacts" | Out-Null

$DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1"
Invoke-WebRequest $DOTNET_INSTALL_SCRIPT_URL -OutFile "$RepoRoot\artifacts\dotnet-install.ps1"

& "$RepoRoot\artifacts\dotnet-install.ps1" -Verbose -Version 1.0.4
if($LASTEXITCODE -ne 0) { throw "Failed to install dotnet cli" }

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

if (-not $env:BUILD_NUMBER)
{
  $env:BUILD_NUMBER = 0
}

if (-not $env:PACKAGE_VERSION)
{
  $env:PACKAGE_VERSION = "1.0.0"
}

$NoTimestampPackageVersion=$env:PACKAGE_VERSION

if (-not $env:BUILD_QUALITY)
{
  $env:BUILD_QUALITY = "beta1"
}

$NoTimestampPackageVersion=$env:PACKAGE_VERSION + "-" + $env:BUILD_QUALITY

$TimestampPackageVersion=$NoTimestampPackageVersion + "-" + [System.DateTime]::Now.ToString("yyyyMMdd") + "-" + $env:BUILD_NUMBER

& dotnet msbuild $RepoRoot\build.proj /p:IsFullFrameworkBuildSupported=$PerformFullFrameworkBuild /p:New3RuntimeIdentifier=$Runtime /p:Configuration=$Configuration
& $RepoRoot\SetPath.ps1 -DevDir "$DevDir"
