#!/bin/bash

RID="$( dotnet --info | grep -Po "(?<=RID:)(?:\s+).*" | grep -Po "\S+" )"
OS="$( echo "$RID" | grep -Po ".*?(?=-)" )"
ARCH="$( echo "$RID" | grep -Po "(?<=-).*" )"

if [ "$OS" == "ubuntu.16.04" ]; then
    OS="ubuntu.14.04"
fi

RID="$OS-$ARCH"

echo "Using RID: $RID"

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
dotnet restore --ignore-failed-sources -v Error

echo Creating directory structure...
mkdir "src/dotnet-new3/bin"
mkdir "src/dotnet-new3/bin/$DN3B"
mkdir "src/dotnet-new3/bin/$DN3B/netcoreapp1.0"
mkdir "src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID"
mkdir "src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns"

cd src

echo Building/Packing core...
cd Microsoft.TemplateEngine.Core
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns" | grep -P --color=never "(Compilation|Error|Warning)"

echo Building/Packing abstractions...
cd ../Microsoft.TemplateEngine.Abstractions
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns" | grep -P --color=never "(Compilation|Error|Warning)"

echo Building/Packing runner...
cd ../Microsoft.TemplateEngine.Runner
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns" | grep -P --color=never "(Compilation|Error|Warning)"

echo Building/Packing VS template support...
cd ../Microsoft.TemplateEngine.Orchestrator.VsTemplates
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns" | grep -P --color=never "(Compilation|Error|Warning)"

echo Building/Packing Runnable Project support...
cd ../Microsoft.TemplateEngine.Orchestrator.RunnableProjects
dotnet pack -c $DN3B -o "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns" | grep -P --color=never "(Compilation|Error|Warning)"

echo Building dotnet new3...
cd ../dotnet-new3
dotnet build -r "$RID" -c $DN3B | grep -P --color=never "(Compilation|Error|Warning)"

echo Importing built in templates...
cp -r "$DN3BASEDIR/template_feed/"* "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/BuiltIns/"
echo Done!

if [ ! -L /usr/local/bin/setup.sh ]; then
echo "Creating symbolic link /usr/local/bin/setup.sh -> $DN3BASEDIR/setup.sh"
sudo ln -s "$DN3BASEDIR/setup.sh" /usr/local/bin/setup.sh
fi

echo You can now use setup.sh from anywhere to rebuild dotnet new3

if [ ! -L /usr/local/bin/harderreset.sh ]; then
echo "Creating symbolic link /usr/local/bin/harderreset.sh -> $DN3BASEDIR/harderreset.sh"
sudo ln -s "$DN3BASEDIR/harderreset.sh" /usr/local/bin/harderreset.sh
fi

echo You can now use harderreset.sh from anywhere to delete dotnet new3 artifacts

if [ ! -L /usr/local/bin/dotnet-new3 ]; then
echo "Creating symbolic link /usr/local/bin/dotnet-new3 -> $DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/dotnet-new3"
sudo ln -s "$DN3BASEDIR/src/dotnet-new3/bin/$DN3B/netcoreapp1.0/$RID/dotnet-new3" /usr/local/bin/dotnet-new3 
fi

echo dotnet new3 is ready!