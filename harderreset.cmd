@echo off
CALL "%~dp0\hardreset.cmd"

rmdir "%~dp0\artifacts" /S/Q 2> nul
rmdir "%~dp0\dev" /S/Q 2> nul
rmdir "%~dp0\src\dotnet-new3\bin" /S/Q 2> nul
rmdir "%~dp0\src\dotnet-new3\obj" /S/Q 2> nul
