@echo off
SET DN3BASEDIR=%~dp0

PUSHD .
SET DN3B=Release
echo Using build configuration "%DN3B%"...

powershell -NoProfile -NoLogo -Command "& \"%~dp0tools\AcquireDotnet.ps1\" %*; exit $LastExitCode;"
if %errorlevel% neq 0 exit /b %errorlevel%

CALL "%~dp0\harderreset.cmd"

mkdir %~dp0\dev 1>nul

echo Building for full framework
%~dp0\.dotnet\dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build /p:TargetFramework=net46 /p:Configuration=Release

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)

echo Building for .NET Core
%~dp0\.dotnet\dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build;RunTests /p:TargetFramework=netcoreapp2.1 /p:Configuration=Release

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)
