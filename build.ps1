#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [switch]$Help)

if($Help)
{
    Write-Host "Usage: .\build.ps1 [-Configuration <CONFIGURATION>] [-Help]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Configuration <CONFIGURATION>     Build the specified Configuration (Debug or Release, default: Debug)"
    Write-Host "  -Help                              Display this help message"
    exit 0
}

$ProjectsToPack = @(
    "Microsoft.TemplateEngine.Utils",
    "Microsoft.TemplateEngine.Core.Contracts",
    "Microsoft.TemplateEngine.Core",
    "Microsoft.TemplateEngine.Edge",
    "Microsoft.TemplateEngine.Abstractions",
    "Microsoft.TemplateEngine.Orchestrator.RunnableProjects"
 )

$TestProjects = @(
    "Microsoft.TemplateEngine.Core.UnitTests"
)

$RepoRoot = "$PSScriptRoot"
$PackagesDir = "$RepoRoot\artifacts\packages"
$env:CONFIGURATION = $Configuration;

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
if (!$env:DOTNET_INSTALL_DIR)
{
    $env:DOTNET_INSTALL_DIR="$RepoRoot\.dotnet\"
}

if (!(Test-Path $env:DOTNET_INSTALL_DIR))
{
    mkdir $env:DOTNET_INSTALL_DIR | Out-Null
}

if (!(Test-Path "$RepoRoot\artifacts"))
{
    mkdir "$RepoRoot\artifacts" | Out-Null
}

$DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1"
Invoke-WebRequest $DOTNET_INSTALL_SCRIPT_URL -OutFile "$RepoRoot\artifacts\dotnet-install.ps1"

& "$RepoRoot\artifacts\dotnet-install.ps1" -Verbose
if($LASTEXITCODE -ne 0) { throw "Failed to install dotnet cli" }

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

# Restore
Write-Host "Restoring all src projects..."
dotnet restore "$RepoRoot\src"

Write-Host "Restoring all test projects..."
dotnet restore "$RepoRoot\test"

Write-Host "Build dotnet new3..."
dotnet build "$RepoRoot\src\dotnet-new3\project.json" -c $Configuration

foreach ($ProjectName in $ProjectsToPack) {
    $ProjectFile = "$RepoRoot\src\$ProjectName\project.json"

    & dotnet pack "$ProjectFile" --output "$PackagesDir" --configuration "$env:CONFIGURATION"
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

Write-Host "Running tests..."
foreach ($ProjectName in $TestProjects) {
    $ProjectFile = "$RepoRoot\test\$ProjectName\project.json"
    $TestResultFile = "$RepoRoot\test\$ProjectName\$ProjectFile-testResults.xml"

    & dotnet test "$ProjectFile" --configuration "$env:CONFIGURATION" -xml "$TestResultFile"
    if (!$?) {
        Write-Host "dotnet test failed for: $ProjectFile"
        Exit 1
    }
}