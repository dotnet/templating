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

 $TestProjectsToPack = @(
    "Microsoft.TemplateEngine.Mocks"
 )

$TestProjects = @(
    "Microsoft.TemplateEngine.Core.UnitTests",
    "Microsoft.TemplateEngine.Utils.UnitTests",
    "dotnet-new3.UnitTests"
)

$RepoRoot = "$PSScriptRoot"
$PackagesDir = "$RepoRoot\artifacts\packages"
$PackagesNoTimeStampDir = "$RepoRoot\artifacts\packages-notimestamp"
$TemplatesNoTimeStampDir = "$RepoRoot\artifacts\templates-notimestamp"
$DevDir = "$RepoRoot\dev"
$env:CONFIGURATION = $Configuration;

rm "$DevDir" -Force -Recurse
if(Test-Path "$DevDir") { throw "Failed to remove 'dev'" }
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

if (Test-Path "$RepoRoot\artifacts")
{
    rm "$RepoRoot\artifacts" -Force -Recurse
}

mkdir "$RepoRoot\artifacts" | Out-Null

$DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.ps1"
Invoke-WebRequest $DOTNET_INSTALL_SCRIPT_URL -OutFile "$RepoRoot\artifacts\dotnet-install.ps1"

& "$RepoRoot\artifacts\dotnet-install.ps1" -Verbose
if($LASTEXITCODE -ne 0) { throw "Failed to install dotnet cli" }

# Put the stage0 on the path
$env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"

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

if (-not $env:BUILD_NUMBER)
{
  $env:BUILD_NUMBER = 0
}

# Build timestamp packages if a build number was set in the environment
foreach ($ProjectName in $ProjectsToPack) {
    Write-Host "Packing (timestamp) $ProjectName..."

    $ProjectFile = "$RepoRoot\src\$ProjectName\$ProjectName.csproj"

    & dotnet pack "$ProjectFile" --output "$PackagesDir" --configuration "$env:CONFIGURATION" /p:CreateTimestampPackages=true
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

foreach ($ProjectName in $ProjectsToPack) {
    Write-Host "Packing (no-timestamp) $ProjectName..."

    $ProjectFile = "$RepoRoot\src\$ProjectName\$ProjectName.csproj"

    & dotnet pack "$ProjectFile" --output "$PackagesNoTimeStampDir" --configuration "$env:CONFIGURATION" --no-build
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

$x = PWD
# Restore
Write-Host "Restoring dotnet new3..."
cd "$RepoRoot\src\dotnet-new3"
& dotnet msbuild /t:Restore "/p:RuntimeIdentifier=win7-x86;TargetFramework=netcoreapp1.0;RestoreRecursive=False;CreateTimestampPackages=true"
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}
cd $x

Write-Host "Publishing dotnet-new3..."
& dotnet publish "$RepoRoot\src\dotnet-new3\dotnet-new3.csproj" -c $Configuration -r $Runtime -f netcoreapp1.0 -o "$DevDir" -p:CreateTimestampPackages=true
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}

Write-Host "Cleaning up after publish..."
rm "$RepoRoot\src\dotnet-new3\bin\$Configuration\netcoreapp1.0\*.*" -Force

Write-Host "Packaging templates (timestamp)..."
& dotnet msbuild "$RepoRoot\template_feed\Template.proj" /p:CreateTimestampPackages=true
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}

Write-Host "Packaging templates (no timestamp)..."
& dotnet msbuild "$RepoRoot\template_feed\Template.proj" /p:CreateTimestampPackages=false /p:PackOutput="$TemplatesNoTimeStampDir"
if ($LastExitCode -ne 0)
{
    exit $LastExitCode
}

#Write-Host "Restoring mocks..."
foreach ($ProjectName in $TestProjectsToPack) {
    $ProjectFile = "$RepoRoot\test\$ProjectName\$ProjectName.csproj"

    & dotnet restore "$ProjectFile"
    if (!$?) {
        Write-Host "dotnet restore failed for: $ProjectFile"
        Exit 1
    }
}

#Write-Host "Packing mocks..."
foreach ($ProjectName in $TestProjectsToPack) {
    Write-Host "Packing (timestamp) $ProjectName..."

    $ProjectFile = "$RepoRoot\test\$ProjectName\$ProjectName.csproj"

    & dotnet pack "$ProjectFile" --output "$PackagesDir" --configuration "$env:CONFIGURATION" /p:CreateTimestampPackages=true
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

foreach ($ProjectName in $TestProjectsToPack) {
    Write-Host "Packing (no-timestamp) $ProjectName..."

    $ProjectFile = "$RepoRoot\test\$ProjectName\$ProjectName.csproj"

    & dotnet pack "$ProjectFile" --output "$PackagesNoTimeStampDir" --configuration "$env:CONFIGURATION" --no-build
    if (!$?) {
        Write-Host "dotnet pack failed for: $ProjectFile"
        Exit 1
    }
}

#Write-Host "Restoring all test projects..."
foreach ($ProjectName in $TestProjects) {
    $ProjectFile = "$RepoRoot\test\$ProjectName\$ProjectName.csproj"

    & dotnet restore "$ProjectFile"
    if (!$?) {
        Write-Host "dotnet restore failed for: $ProjectFile"
        Exit 1
    }
}

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