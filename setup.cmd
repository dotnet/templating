@echo off

doskey dn3setup="%~dp0\setup.cmd"

PUSHD %~dp0\src
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...

echo Restoring all packages...
dotnet restore 1>nul

echo Building core...

cd dotnet-new3
dotnet build -r win10-x64 -c %DN3B% 1>nul

echo Building runner...
cd ..\Mutant.Chicken.Runner
dotnet build -c %DN3B% 1>nul

echo Copying runner...
copy bin\%DN3B%\netstandard1.5\Mutant.Chicken.Runner.dll ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Mutant.Chicken.Runner.dll 1>nul

echo Building VS Template support...
cd ..\Mutant.Chicken.Orchestrator.VsTemplates
dotnet build -c %DN3B% 1>nul

echo Copying VS Template Support...
copy bin\%DN3B%\netstandard1.5\Mutant.Chicken.Orchestrator.VsTemplates.dll ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Mutant.Chicken.Orchestrator.VsTemplates.dll 1>nul

echo Building Runnable Project support...
cd ..\Mutant.Chicken.Orchestrator.RunnableProjects
dotnet build -c %DN3B% 1>nul

echo Copying Runnable Project Support...
copy bin\%DN3B%\netstandard1.5\Mutant.Chicken.Orchestrator.RunnableProjects.dll ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Mutant.Chicken.Orchestrator.RunnableProjects.dll 1>nul
cd ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64

echo Artifacts built and placed.

echo Updating path...
SET PATH=%CD%;%PATH%

echo Resetting to defaults...
dotnet new3 -u *

echo Registering core components...
dotnet new3 -i "dotnet-new3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
echo Registering VS Template components...
dotnet new3 -i "Mutant.Chicken.Orchestrator.VsTemplates, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
echo Registering Runnable Project components...
dotnet new3 -i "Mutant.Chicken.Orchestrator.RunnableProjects, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"

echo Updating sources...
dotnet new3 -i "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\ProjectTemplates"
dotnet new3 -i "c:\RunnableProjectTemplates"

echo Done.
POPD

echo You can now use `dn3setup` from anywhere (in this console session) to run setup again.

@echo on
dotnet new3 -c