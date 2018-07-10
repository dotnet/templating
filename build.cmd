@echo off
SET DN3BASEDIR=%~dp0

PUSHD %~dp0\src
SET DN3B=Release
echo Using build configuration "%DN3B%"...

IF "%DN3FFB%" == "" (SET DN3FFB=$true)

CALL "%~dp0\harderreset.cmd"

mkdir %~dp0\dev 1>nul

echo Building for full framework
dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build /p:TargetFramework=net46 /p:Configuration=%DN3B% %*

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)

echo Building for .NET Core
dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build;Pack;RunTests /p:TargetFramework=netcoreapp2.1 /p:Configuration=%DN3B% %*

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)
