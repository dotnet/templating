@echo off
%~dp0\.dotnet\dotnet msbuild %~dp0\build\CoreBuild.proj /t:PackAll /v:diag /p:Configuration=Release

if %ERRORLEVEL% NEQ 0 (
    echo BUILD FAILED
    exit /B
)
