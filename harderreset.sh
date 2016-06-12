#!/bin/bash

CWD="$( pwd )"

if ["$DN3BASEDIR" == ""]; then
DN3BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
fi

rm -rf "$DN3BASEDIR\src\dotnet-new3\bin"
rm -rf "$DN3BASEDIR\src\dotnet-new3\obj"
rm -rf "~/.netnew"
