@echo off
PUSHD %~dp0\src
dotnet restore
cd dotnet-new3
IF "%DN3B%" == "" (SET DN3B=Release)
dotnet build -r win10-x64 -c %DN3B%
cd ..\Mutant.Chicken.Orchestrator.VsTemplates
dotnet build -c %DN3B%
copy bin\%DN3B%\netstandard1.5\Mutant.Chicken.Runner.dll ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Mutant.Chicken.Runner.dll
copy bin\%DN3B%\netstandard1.5\Mutant.Chicken.Orchestrator.VsTemplates.dll ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64\Mutant.Chicken.Orchestrator.VsTemplates.dll
cd ..\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win10-x64
SET PATH=%CD%;%PATH%
dotnet new3 reset
dotnet new3 component add "dotnet-new3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
dotnet new3 component add "Mutant.Chicken.Orchestrator.VsTemplates, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
dotnet new3 source add VSProj "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\ProjectTemplates"
POPD