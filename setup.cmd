@echo off

doskey setup="%~dp0\setup.cmd"
doskey build="%~dp0\dn3build.cmd"
doskey debug="%~dp0\dn3buildmode-debug.cmd"
doskey release="%~dp0\dn3buildmode-release.cmd"
doskey hardreset="%~dp0\hardreset.cmd"
doskey harderreset="%~dp0\harderreset.cmd"
doskey bundle="%~dp0\bundle.cmd"
doskey bin="%~dp0\bin.cmd"

SET DN3BASEDIR=%~dp0

PUSHD %~dp0\src
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...

CALL "%~dp0\harderreset.cmd"

echo Creating developer environment...
echo %~dp0
if EXIST %~dp0\dev (
    RMDIR %~dp0\dev /S /Q
    DEL %~dp0dev /Q
)

mkdir %~dp0\dev 1>nul

echo "Calling build.ps1"
powershell -NoProfile -NoLogo -Command "& \"%~dp0build.ps1\" -Configuration %DN3B%; exit $LastExitCode;"

echo Artifacts built and placed.

SET DN3BINDIR=
for /f %%f in ('dir /AD /B "%~dp0\src\dotnet-new3\bin\%DN3B%"') do SET DN3BINDIR="%~dp0\src\dotnet-new3\bin\%DN3B%\%%f"

echo %DN3BINDIR%

cd %~dp0\dev

SET DN3=%~dp0\dev

echo Updating path...
IF "%OLDPATH%" == "" (SET "OLDPATH=%PATH%")
SET "PATH=%CD%;%~dp0\.dotnet;%OLDPATH%"

echo Copying configuration for builtins...
COPY %~dp0\src\dotnet-new3\defaultinstall.*.list %~dp0\dev /Y 1>nul

echo Deleting NuGet caches...
for /f %%f in ('dir /AD /B "%USERPROFILE%\.nuget\packages\Microsoft.TemplateEngine.*"') do RMDIR "%USERPROFILE%\.nuget\packages\%%f" /S /Q
for /f %%f in ('dir /AD /B "%USERPROFILE%\.nuget\packages\Microsoft.DotNet.*.Templates.*"') do RMDIR "%USERPROFILE%\.nuget\packages\%%f" /S /Q

echo Done.
POPD

echo.
echo You can now use `setup` from anywhere (in this console session) to run setup again.
echo You can now use `build` from anywhere (in this console session) to build dotnet-new3 in the current configuration.
echo You can now use `debug` from anywhere (in this console session) to change the active build configuration to DEBUG.
echo You can now use `release` from anywhere (in this console session) to change the active build configuration to RELEASE.
echo.
echo dotnet new3 is ready!