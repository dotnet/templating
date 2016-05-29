@echo off
SET DN3B=Debug
CALL %~dp0\harderreset.cmd
SET DN3B=Release
CALL %~dp0\harderreset.cmd
CALL %~dp0\setup.cmd

dotnet publish %~dp0\src\dotnet-new3 -c Release -r win10-x64 -o . --no-build
MKDIR Builtins
COPY %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ BuiltIns /Y
COPY %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\defaultinstall.*.list . /Y
