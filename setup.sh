#!/bin/bash

RID="$( dotnet --info | egrep -e 'RID:' | egrep -o -e ':.*' | egrep -o -e '[^:]+' | egrep -o -e '(\w|\.|-)+' )"
OS="$( echo "$RID" | egrep -o -e .*- | egrep -o -e [^-]+ )"
ARCH="$( echo "$RID" | egrep -o -e -.* | egrep -o -e [^-]+ )"
REPOROOT="$( cd -P "$( dirname "$SOURCE" )" && pwd )"

if [ "$OS" == "ubuntu.16.04" ]; then
    OS="ubuntu.14.04"
    SPECIAL_OS="YES"
fi

if [ "$SPECIAL_OS" == "YES" ]; then
    RID="$OS-$ARCH"
fi

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

$REPOROOT/build.sh -c $DN3B -r $RID

echo Importing built in templates...
cp -r "$REPOROOT/template_feed/"* "$REPOROOT/dev/BuiltIns/"
echo Done!

if [ ! -L /usr/local/bin/setup.sh ]; then
echo "Creating symbolic link /usr/local/bin/setup.sh -> $REPOROOT/setup.sh"
ln -s "$REPOROOT/setup.sh" /usr/local/bin/setup.sh
fi

echo You can now use setup.sh from anywhere to rebuild dotnet new3

if [ ! -L /usr/local/bin/harderreset.sh ]; then
echo "Creating symbolic link /usr/local/bin/harderreset.sh -> $REPOROOT/harderreset.sh"
ln -s "$REPOROOT/harderreset.sh" /usr/local/bin/harderreset.sh
fi

echo You can now use harderreset.sh from anywhere to delete dotnet new3 artifacts

if [ ! -L /usr/local/bin/dotnet-new3 ]; then
echo "Creating symbolic link /usr/local/bin/dotnet-new3 -> $REPOROOT/dev/dotnet-new3"
ln -s "$REPOROOT/dev/dotnet-new3" /usr/local/bin/dotnet-new3
fi

cd $CWD

echo dotnet new3 is ready!
