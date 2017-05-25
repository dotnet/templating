#!/bin/bash

RID="$( dotnet --info | egrep -e 'RID:' | egrep -o -e ':.*' | egrep -o -e '[^:]+' | egrep -o -e '(\w|\.|-)+' )"
OS="$( echo "$RID" | egrep -o -e .*- | egrep -o -e [^-]+ )"
ARCH="$( echo "$RID" | egrep -o -e -.* | egrep -o -e [^-]+ )"
REPOROOT="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

echo "Using RID: $RID"
echo "Using OS: $OS"

CWD="$( pwd )"

if [ -z $DN3B ]; then
DN3B="Debug"; export DN3B
fi

cd "$REPOROOT"

echo Using build configuration "$DN3B"
/bin/bash $REPOROOT/harderreset.sh

echo Creating directory structure...
mkdir dev
mkdir dev/BuiltIns

$REPOROOT/build.sh -c $DN3B

echo Importing built in templates...
cp -r "$REPOROOT/template_feed/"* "$REPOROOT/dev/BuiltIns/"
echo Done!

if [ ! -L /usr/local/bin/setup.sh ]; then
echo "Creating symbolic link /usr/local/bin/setup.sh -> $REPOROOT/setup.sh"
sudo ln -s "$REPOROOT/setup.sh" /usr/local/bin/setup.sh
chmod 777 "$REPOROOT/setup.sh"
fi

echo You can now use setup.sh from anywhere to rebuild dotnet new3

if [ ! -L /usr/local/bin/harderreset.sh ]; then
echo "Creating symbolic link /usr/local/bin/harderreset.sh -> $REPOROOT/harderreset.sh"
sudo ln -s "$REPOROOT/harderreset.sh" /usr/local/bin/harderreset.sh
chmod 777 "$REPOROOT/harderreset.sh"
fi

echo You can now use harderreset.sh from anywhere to delete dotnet new3 artifacts

if [ ! -L /usr/local/bin/dotnet-new3 ]; then
echo "Creating symbolic link /usr/local/bin/dotnet-new3 -> $REPOROOT/dev/dotnet-new3.sh"
sudo ln -s "$REPOROOT/dev/dotnet-new3.sh" /usr/local/bin/dotnet-new3
chmod 777 "$REPOROOT/dev/dotnet-new3.sh"
fi

cd $CWD

echo dotnet new3 is ready!
