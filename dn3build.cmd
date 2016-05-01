@echo off
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...
dotnet build %~dp0\src\dotnet-new3 -r win10-x64 -c %DN3B%