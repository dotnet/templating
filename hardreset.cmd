@echo off

rmdir "%userprofile%\.netnew" /S/Q 2> nul
rmdir "%userprofile%\.templateengine\dotnetcli-preview" /S/Q 2> nul
rmdir "%userprofile%\.templateengine\endtoendtestharness" /S/Q 2> nul
