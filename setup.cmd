@echo off
cd src\dotnet-new3
dotnet restore
dotnet build -r win10-x64
cd ..\Mutant.Chicken.Orchestrator.VsTemplates
dotnet restore
dotnet build
copy bin\Debug\netstandard1.5\Mutant.Chicken.Runner.dll ..\dotnet-new3\bin\Debug\netcoreapp1.0\win10-x64\Mutant.Chicken.Runner.dll
copy bin\Debug\netstandard1.5\Mutant.Chicken.Orchestrator.VsTemplates.dll ..\dotnet-new3\bin\Debug\netcoreapp1.0\win10-x64\Mutant.Chicken.Orchestrator.VsTemplates.dll
cd ..\dotnet-new3
SET PATH=%PATH%;%CD%
dotnet new3 reset
dotnet new3 component add "dotnet-new3, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
dotnet new3 component add "Mutant.Chicken.Orchestrator.VsTemplates, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
dotnet new3 source add VSProj "C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\ProjectTemplates"
