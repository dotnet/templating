@echo off

doskey setup="%~dp0\setup.cmd"
doskey build="%~dp0\dn3build.cmd"
doskey debug="%~dp0\dn3buildmode-debug.cmd"
doskey release="%~dp0\dn3buildmode-release.cmd"
doskey hardreset="%~dp0\hardreset.cmd"
doskey harderreset="%~dp0\harderreset.cmd"

SET DN3BASEDIR=%~dp0

PUSHD %~dp0\src
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...

echo Restoring all packages...
dotnet restore --infer-runtimes --ignore-failed-sources 1>nul

echo Building dotnet new3...
cd dotnet-new3
dotnet build -r win10-x64 -c %DN3B% 1>nul

echo Creating local feed...
mkdir %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns

echo Building core...
cd ..\Microsoft.TemplateEngine.Core
dotnet build -c %DN3B% 1>nul
echo Packing core...
dotnet pack -c %DN3B% -o %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ 1>nul

echo Building abstractions...
cd ..\Microsoft.TemplateEngine.Abstractions
dotnet build -c %DN3B% 1>nul
echo Packing abstractions...
dotnet pack -c %DN3B% -o %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ 1>nul

echo Building runner...
cd ..\Microsoft.TemplateEngine.Runner
dotnet build -c %DN3B% 1>nul
echo Packing runner...
dotnet pack -c %DN3B% -o %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ 1>nul

echo Building VS Template support...
cd ..\Microsoft.TemplateEngine.Orchestrator.VsTemplates
dotnet build -c %DN3B% 1>nul
echo Packing VS Template Support...
dotnet pack -c %DN3B% -o %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ 1>nul

echo Building Runnable Project support...
cd ..\Microsoft.TemplateEngine.Orchestrator.RunnableProjects
dotnet build -c %DN3B% 1>nul
echo Packing Runnable Project Support...
dotnet pack -c %DN3B% -o %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns\ 1>nul

echo Artifacts built and placed.

cd %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\

echo Updating path...
IF "%OLDPATH%" == "" (SET "OLDPATH=%PATH%")
SET "PATH=%CD%;%OLDPATH%"

echo Copying templates...
COPY %~dp0\template_feed\*.nupkg %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\BuiltIns /Y 1>nul

echo Copying configuration for builtins...
COPY %~dp0\src\dotnet-new3\defaultinstall.*.list %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64 /Y 1>nul

echo Done.
POPD

echo.
echo You can now use `setup` from anywhere (in this console session) to run setup again.
echo You can now use `build` from anywhere (in this console session) to build dotnet-new3 in the current configuration.
echo You can now use `debug` from anywhere (in this console session) to change the active build configuration to DEBUG.
echo You can now use `release` from anywhere (in this console session) to change the active build configuration to RELEASE.
echo.
echo dotnet new3 is ready!