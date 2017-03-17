@echo off
SET DN3B=Release
echo dotnet-new3 build configuration set to %DN3B%
PUSHD %~DP0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64
IF "%OLDPATH%" == "" (SET "OLDPATH=%PATH%")
SET "PATH=%CD%;%OLDPATH%"
POPD
