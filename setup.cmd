@echo off

doskey setup="%~dp0\setup.cmd"
doskey build="%~dp0\dn3build.cmd"
doskey debug="%~dp0\dn3buildmode-debug.cmd"
doskey release="%~dp0\dn3buildmode-release.cmd"

PUSHD %~dp0\src
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...

echo Restoring all packages...
dotnet restore --infer-runtimes --ignore-failed-sources 1>nul

echo Building dotnet new3...
cd dotnet-new3
dotnet build -r win10-x64 -c %DN3B% 1>nul

echo Building core...
cd ..\Mutant.Chicken.Core
dotnet build -c %DN3B% 1>nul

echo Packing core...
dotnet pack -c %DN3B% -o %~dp0\feed 1>nul

echo Building abstractions...
cd ..\Mutant.Chicken.Abstractions
dotnet build -c %DN3B% 1>nul

echo Packing abstractions...
dotnet pack -c %DN3B% -o %~dp0\feed 1>nul

echo Building runner...
cd ..\Mutant.Chicken.Runner
dotnet build -c %DN3B% 1>nul

echo Packing runner...
dotnet pack -c %DN3B% -o %~dp0\feed 1>nul

echo Building VS Template support...
cd ..\Mutant.Chicken.Orchestrator.VsTemplates
dotnet build -c %DN3B% 1>nul
echo Packing VS Template Support...
dotnet pack -c %DN3B% -o %~dp0\feed 1>nul

echo Building Runnable Project support...
cd ..\Mutant.Chicken.Orchestrator.RunnableProjects
dotnet build -c %DN3B% 1>nul
echo Packing Runnable Project Support...
dotnet pack -c %DN3B% -o %~dp0\feed 1>nul

echo Artifacts built and placed.

cd %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\

echo Updating path...
IF "%OLDPATH%" == "" (SET "OLDPATH=%PATH%")
SET "PATH=%CD%;%OLDPATH%"

IF "%DN3RESET%" == "" (
echo Resetting to defaults...
dotnet new3 -u *

echo Updating sources...
mkdir %~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Templates
dotnet new3 -i %~dp0src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Templates
SET DN3RESET=Done
)

echo Done.
POPD

echo.
echo You can now use `setup` from anywhere (in this console session) to run setup again.
echo You can now use `build` from anywhere (in this console session) to build dotnet-new3 in the current configuration.
echo You can now use `debug` from anywhere (in this console session) to change the active build configuration to DEBUG.
echo You can now use `release` from anywhere (in this console session) to change the active build configuration to RELEASE.

@echo on
dotnet new3 -c