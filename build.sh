#!/usr/bin/env bash
#
# Copyright (c) .NET Foundation and contributors. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.
#

set -e

SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done
REPOROOT="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
export CONFIGURATION="Debug"

source "$REPOROOT/scripts/common/_prettyprint.sh"

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -c|--configuration)
            export CONFIGURATION=$2
            shift
            ;;
        --help)
            echo "Usage: $0 [--configuration <CONFIGURATION>] [--help]"
            echo ""
            echo "Options:"
            echo "  --configuration <CONFIGURATION>      Build the specified Configuration (Debug or Release, default: Debug)"
            echo "  --help                               Display this help message"
            exit 0
            ;;
        *)
            break
            ;;
    esac

    shift
done

rm -rf $REPOROOT/artifacts

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR" ] && export DOTNET_INSTALL_DIR=$REPOROOT/.dotnet
[ -d "$DOTNET_INSTALL_DIR" ] || mkdir -p $DOTNET_INSTALL_DIR

[ -d "$REPOROOT/artifacts" ] || mkdir -p $REPOROOT/artifacts

[ -z $NUGET_PACKAGES ] && export NUGET_PACKAGES="$REPOROOT/.nuget/packages"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.sh"
curl -sSL "$DOTNET_INSTALL_SCRIPT_URL" | bash /dev/stdin --verbose --version 1.0.4

# Put stage 0 on the PATH (for this shell only)
PATH="$DOTNET_INSTALL_DIR:$PATH"

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    echo "Increasing file description limit to 1024"
    ulimit -n 1024
fi

export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
$DOTNET_INSTALL_DIR/dotnet msbuild "$REPOROOT/build.proj" /p:Configuration=$CONFIGURATION
