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

if [ -z $DN3B ]; then
DN3B="Debug"; export DN3B
fi

cd "$DN3BASEDIR"

echo Using build configuration "$DN3B"
/bin/bash harderreset.sh

echo Creating directory structure...
mkdir dev
mkdir dev/BuiltIns

$REPOROOT/build.sh -c $DN3B -r $RID

echo Importing built in templates...
cp -r "$DN3BASEDIR/template_feed/"* "$DN3BASEDIR/dev/BuiltIns/"
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
echo "Creating symbolic link /usr/local/bin/dotnet-new3 -> $DN3BASEDIR/dev'/dotnet-new3"
sudo ln -s "$DN3BASEDIR/dev/dotnet-new3" /usr/local/bin/dotnet-new3 
fi

CWD2="$( echo $CWD | sed -e 's/\//\\\//g' )"
cd $DN3BASEDIR/dev
sed -i 's/\\/\//g' defaultinstall.*.list
sed -i "s/%DN3%\/../$CWD2/g" defaultinstall.*.list
cd $CWD

echo dotnet new3 is ready!
