#!/bin/bash

CWD="$( pwd )"
DN3BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
if [ "$PRESETDN3BASEDIR" == "" ]; then
PRESETDN3BASEDIR="$DN3BASEDIR"; export PRESETDN3BASEDIR
else
DN3BASEDIR="$PRESETDN3BASEDIR"
fi

if [ "$DN3B" == "" ]; then
DN3B="Debug"; export DN3B
fi 

cd "$DN3BASEDIR"

echo Using build configuration "$DN3B"
/bin/bash harderreset.sh

echo Restoring all packages...
dotnet restore --infer-runtimes --ignore-failed-sources > /dev/null 2>&1

echo Building dotnet new3...
cd src/dotnet-new3
dotnet build -r ubuntu.14.04-x64 -c $DN3B > /dev/null 2>&1

echo "Creating local feed..."
if [ -e "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" ]; then
    rm -rf "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"
fi

mkdir "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building core...
cd ../Microsoft.TemplateEngine.Core
dotnet build -c $DN3B > /dev/null 2>&1
echo Packing core...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" > /dev/null 2>&1

echo Building abstractions...
cd ../Microsoft.TemplateEngine.Abstractions
dotnet build -c $DN3B > /dev/null 2>&1
echo Packing abstractions...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" > /dev/null 2>&1

echo Building runner...
cd ../Microsoft.TemplateEngine.Runner
dotnet build -c $DN3B > /dev/null 2>&1
echo Packing runner...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" > /dev/null 2>&1

echo Building VS template support...
cd ../Microsoft.TemplateEngine.Orchestrator.VsTemplates
dotnet build -c $DN3B > /dev/null 2>&1
echo Packing VS template support...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" > /dev/null 2>&1

echo Building Runnable Project support...
cd ../Microsoft.TemplateEngine.Orchestrator.RunnableProjects
dotnet build -c $DN3B > /dev/null 2>&1
echo Packing Runnable Project support...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" > /dev/null 2>&1

if [ -L /usr/local/bin/setup.sh ]; then
rm -f /usr/local/bin/setup.sh
fi

ln -s "$DN3BASEDIR/setup.sh" /usr/local/bin/setup.sh

if [ -L /usr/local/bin/dotnet-new3 ]; then
rm -f /usr/local/bin/dotnet-new3
fi

ln -s "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/dotnet-new3" /usr/local/bin/dotnet-new3 
