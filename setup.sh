#!/bin/bash

CWD="$( pwd )"
DN3BASEDIR0="$( realpath ${BASH_SOURCE[0]} )"
DN3BASEDIR1="$( dirname $DN3BASEDIR0 )"
DN3BASEDIR="$( cd $DN3BASEDIR1 && pwd )"

if [ -z $DN3B ]; then
DN3B="Debug"; export DN3B
fi 

cd "$DN3BASEDIR"

echo Using build configuration "$DN3B"
/bin/bash harderreset.sh

echo Restoring all packages...
dotnet restore --ignore-failed-sources

echo Building dotnet new3...
cd src/dotnet-new3
dotnet build -r ubuntu.14.04-x64 -c $DN3B

echo "Creating local feed..."
if [ -e "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns" ]; then
    rm -rf "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"
fi

mkdir "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building core...
cd ../Microsoft.TemplateEngine.Core
dotnet build -c $DN3B
echo Packing core...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building abstractions...
cd ../Microsoft.TemplateEngine.Abstractions
dotnet build -c $DN3B
echo Packing abstractions...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building runner...
cd ../Microsoft.TemplateEngine.Runner
dotnet build -c $DN3B
echo Packing runner...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building VS template support...
cd ../Microsoft.TemplateEngine.Orchestrator.VsTemplates
dotnet build -c $DN3B
echo Packing VS template support...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

echo Building Runnable Project support...
cd ../Microsoft.TemplateEngine.Orchestrator.RunnableProjects
dotnet build -c $DN3B
echo Packing Runnable Project support...
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns"

cp -r "$DN3BASEDIR/template_feed/"* "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/BuiltIns/"

if [ ! -L /usr/local/bin/setup.sh ]; then
ln -s "$DN3BASEDIR/setup.sh" /usr/local/bin/setup.sh
fi

if [ ! -L /usr/local/bin/dotnet-new3 ]; then
ln -s "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/ubuntu.14.04-x64/dotnet-new3" /usr/local/bin/dotnet-new3 
fi
