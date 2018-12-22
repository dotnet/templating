@echo off

doskey setup="%~dp0\setup.cmd"
doskey build="%~dp0\build.cmd"
doskey debug="%~dp0\dn3buildmode-debug.cmd"
doskey release="%~dp0\dn3buildmode-release.cmd"
doskey hardreset="%~dp0\hardreset.cmd"
doskey harderreset="%~dp0\harderreset.cmd"

SET DN3BASEDIR=%~dp0

PUSHD %~dp0
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...

powershell -NoProfile -NoLogo -Command "& \"%~dp0tools\AcquireDotnet.ps1\" %*; exit $LastExitCode;"
if %errorlevel% neq 0 exit /b %errorlevel%

CALL "%~dp0\harderreset.cmd"

mkdir %~dp0\dev 1>nul

echo Building for full framework
%~dp0\.dotnet\dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build /p:TargetFramework=net46 /p:Configuration=%DN3B% %*

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)

echo Building for .NET Core
%~dp0\.dotnet\dotnet msbuild %~dp0\build\CoreBuild.proj /t:GetReady;Restore;Build;Pack;RunTests /p:TargetFramework=netcoreapp2.1 /p:Configuration=%DN3B% %*

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)

echo Artifacts built and placed.

echo Copying configuration for builtins...
COPY %~dp0\src\dotnet-new3\defaultinstall.*.list %~dp0\dev /Y 1>nul

echo Deleting NuGet caches...
for /f %%f in ('dir /AD /B "%USERPROFILE%\.nuget\packages\Microsoft.TemplateEngine.*"') do RMDIR "%USERPROFILE%\.nuget\packages\%%f" /S /Q 2> nul
for /f %%f in ('dir /AD /B "%USERPROFILE%\.nuget\packages\Microsoft.DotNet.*.Templates.*"') do RMDIR "%USERPROFILE%\.nuget\packages\%%f" /S /Q 2> nul

echo Done.
POPD

powershell -NoProfile -NoLogo -Command "& \"%~dp0SetPath.ps1\" -ComputeOnly -DevDir \"%~dp0dev\"; exit $LastExitCode;"
call "%~dp0\artifacts\NewPath.bat"
del "%~dp0\artifacts\NewPath.bat" 2> nul

echo.
echo You can now use `setup` from anywhere (in this console session) to run setup again.
echo You can now use `build` from anywhere (in this console session) to build dotnet-new3 in the current configuration.
echo You can now use `debug` from anywhere (in this console session) to change the active build configuration to DEBUG.
echo You can now use `release` from anywhere (in this console session) to change the active build configuration to RELEASE.
echo.
echo dotnet new3 is ready!
