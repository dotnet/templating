
if not defined FeedTasksPackage (
    set FeedTasksPackage=Microsoft.DotNet.Build.Tasks.Feed
)

if not defined FeedTasksPackageVersion (
    set FeedTasksPackageVersion=1.0.0-prerelease-02203-01
)

if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe" (
    set "MSBuildExePath=%ProgramFiles(x86)%\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin"
)

set PublishToolsPath=%~dp0

set "PATH=%PATH%;%MSBuildExePath%"

msbuild "%PublishToolsPath%\Publish.csproj" /t:Restore

msbuild "%PublishToolsPath%\publish.proj" /p:Configuration=Release /t:Publish %*