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
DDIR="$REPOROOT/dev"
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

echo "Build dotnet new3..."
THISDIR=$(pwd)
cd $REPOROOT/src/dotnet-new3
dotnet msbuild "$REPOROOT/src/dotnet-new3/dotnet-new3.csproj" /t:Restore "/p:TargetFramework=netcoreapp1.0;RestoreRecursive=False;RestoreSources=$REPOROOT/artifacts"
dotnet publish "$REPOROOT/src/dotnet-new3/dotnet-new3.csproj" -c $CONFIGURATION -f netcoreapp1.0 -o "$DDIR"
cd $THISDIR
