#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

param(
    [string]$Configuration="Debug",
    [string]$Runtime="win7-x86",
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
    "Microsoft.TemplateEngine.Core.UnitTests",
    "Microsoft.TemplateEngine.Utils.UnitTests"
)

$RepoRoot = "$PSScriptRoot"
$PackagesDir = "$RepoRoot\artifacts\packages"
$DevDir = "$RepoRoot\dev"
$env:CONFIGURATION = $Configuration;

rm "$DevDir" -Force -Recurse
mkdir "$DevDir"

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

#Write-Host "Restoring all test projects..."
dotnet restore "$RepoRoot\test\Microsoft.TemplateEngine.Core.UnitTests\Microsoft.TemplateEngine.Core.UnitTests.csproj"
dotnet restore "$RepoRoot\test\Microsoft.TemplateEngine.Utils.UnitTests\Microsoft.TemplateEngine.Utils.UnitTests.csproj"

# New Restore

Write-Host "Restoring all projects..."
foreach ($ProjectName in $ProjectsToPack) {
    $ProjectFile = "$RepoRoot\src\$ProjectName\$ProjectName.csproj"

	& dotnet restore "$ProjectFile"
	if (!$?) {
		Write-Host "dotnet restore failed for: $ProjectFile"
		Exit 1
	}
}

Write-Host "Build dependencies..."
dotnet build "$RepoRoot\Microsoft.TemplateEngine.sln" -c $Configuration

foreach ($ProjectName in $ProjectsToPack) {
    Write-Host "Packing $ProjectName..."

    $ProjectFile = "$RepoRoot\src\$ProjectName\$ProjectName.csproj"

    & dotnet pack "$ProjectFile" --output "$PackagesDir" --configuration "$env:CONFIGURATION"
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

$x = PWD
# Restore
Write-Host "Restoring dotnet new3..."
cd "$RepoRoot\src\dotnet-new3"
& dotnet msbuild /t:Restore "/p:RuntimeIdentifier=win7-x86;TargetFramework=netcoreapp1.0;RestoreRecursive=False"
cd $x

Write-Host "Publishing dotnet-new3..."
& dotnet publish "$RepoRoot\src\dotnet-new3\dotnet-new3.csproj" -c $Configuration -r $Runtime -f netcoreapp1.0 -o "$DevDir"

Write-Host "Cleaning up after publish..."
rm "$RepoRoot\src\dotnet-new3\bin\$Configuration\netcoreapp1.0\*.*" -Force

Write-Host "Running tests..."
foreach ($ProjectName in $TestProjects) {
    $ProjectFile = "$RepoRoot\test\$ProjectName\$ProjectName.csproj"
    $TestResultFile = "$ProjectName-testResults.xml"

    & dotnet test "$ProjectFile" --configuration "$env:CONFIGURATION" -l trx
    if (!$?) {
        Write-Host "dotnet test failed for: $ProjectFile"
        Exit 1
    }
}