@echo off

SET DN3BASEDIR=%~dp0

PUSHD %~dp0\src
SET DN3B=Release
echo Using build configuration "%DN3B%"...

SET DN3FFB=$true

CALL "%~dp0\harderreset.cmd"

mkdir %~dp0\dev 1>nul

echo "Calling build.ps1"
powershell -NoProfile -NoLogo -Command "& \"%~dp0build.ps1\" -Configuration %DN3B% -CIBuild $true -TemplatesBuild $true -PerformFullFrameworkBuild %DN3FFB%; exit $LastExitCode;"

echo Done.
POPD
