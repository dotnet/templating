@echo off
IF "%DN3B%" == "" (SET DN3B=Release)
echo Using build configuration "%DN3B%"...
dotnet restore "%~dp0\src\dotnet-new3\dotnet-new3.csproj"
dotnet build "%~dp0\src\dotnet-new3\dotnet-new3.csproj" -c %DN3B%
dotnet publish "%~dp0\src\dotnet-new3\dotnet-new3.csproj" -r win7-x64 -c %DN3B%
del /Q "%~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\*.*"
rmdir /S /Q "%~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\bin"

xcopy /Y /s "%~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win7-x64\publish\*" %~dp0\dev 1>nul
PUSHD %~dp0\dev

rmdir /S /Q "%~dp0\src\dotnet-new3\bin\%DN3B%\netcoreapp1.0\win7-x64"

echo Copying packages...
COPY %~dp0\artifacts\packages\*.nupkg %~dp0\dev\BuiltIns /Y 1>nul
POPD
