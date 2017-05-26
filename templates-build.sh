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
if [ ! "$OS" == "osx.10.11" ]; then
    DN3BASEDIR0="$( realpath ${BASH_SOURCE[0]} )"
else
    DN3BASEDIR0=$BASH_SOURCE[0]
fi
DN3BASEDIR1="$( dirname $DN3BASEDIR0 )"
DN3BASEDIR="$( cd $DN3BASEDIR1 && pwd )"

export DN3B="Release"

cd "$DN3BASEDIR"

echo Using build configuration "$DN3B"
/bin/bash $REPOROOT/harderreset.sh

echo Creating directory structure...
mkdir dev
mkdir dev/BuiltIns

$REPOROOT/build.sh -c $DN3B -r $RID --ci-build --no-engine-build

cd $CWD

echo dotnet new3 is ready!
