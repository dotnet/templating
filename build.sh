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
PACKAGESDIR="$REPOROOT/artifacts/packages"
export CONFIGURATION="Debug"

PROJECTSTOPACK=( \
    Microsoft.TemplateEngine.Utils \
    Microsoft.TemplateEngine.Core.Contracts \
    Microsoft.TemplateEngine.Core \
    Microsoft.TemplateEngine.Edge \
    Microsoft.TemplateEngine.Abstractions \
    Microsoft.TemplateEngine.Orchestrator.RunnableProjects
 )

TESTPROJECTS=( \
    Microsoft.TemplateEngine.Core.UnitTests
)

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

# Use a repo-local install directory (but not the artifacts directory because that gets cleaned a lot
[ -z "$DOTNET_INSTALL_DIR" ] && export DOTNET_INSTALL_DIR=$REPOROOT/.dotnet
[ -d "$DOTNET_INSTALL_DIR" ] || mkdir -p $DOTNET_INSTALL_DIR

[ -d "$REPOROOT/artifacts" ] || mkdir -p $REPOROOT/artifacts

DOTNET_INSTALL_SCRIPT_URL="https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0/scripts/obtain/dotnet-install.sh"
curl -sSL "$DOTNET_INSTALL_SCRIPT_URL" | bash /dev/stdin --verbose

# Put stage 0 on the PATH (for this shell only)
PATH="$DOTNET_INSTALL_DIR:$PATH"

# Increases the file descriptors limit for this bash. It prevents an issue we were hitting during restore
FILE_DESCRIPTOR_LIMIT=$( ulimit -n )
if [ $FILE_DESCRIPTOR_LIMIT -lt 1024 ]
then
    echo "Increasing file description limit to 1024"
    ulimit -n 1024
fi

# Restore
echo "Restoring all src projects..."
dotnet restore "$REPOROOT/src" --ignore-failed-sources

echo "Restoring all test projects..."
dotnet restore "$REPOROOT/test" --ignore-failed-sources

echo "Build abstractions..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Abstractions/project.json" -c $CONFIGURATION -f netstandard1.3

echo "Build Core..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Core/project.json" -c $CONFIGURATION -f netstandard1.3

echo "Build Core Contracts..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Core.Contracts/project.json" -c $CONFIGURATION -f netstandard1.3

echo "Build Runnable Projects..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects/project.json" -c $CONFIGURATION -f netstandard1.3

echo "Build Runnable Utils..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Utils/project.json" -c $CONFIGURATION -f netstandard1.3

echo "Build Edge..."
dotnet build "$REPOROOT/src/Microsoft.TemplateEngine.Edge/project.json" -c $CONFIGURATION -f netcoreapp1.0

echo "Build dotnet new3..."
dotnet build "$REPOROOT/src/dotnet-new3/project.json" -c $CONFIGURATION -f netcoreapp1.0

for projectToPack in ${PROJECTSTOPACK[@]}
do
    dotnet pack "$REPOROOT/src/$projectToPack/project.json" --output "$PACKAGESDIR" --configuration "$CONFIGURATION" --no-build
done

echo "Running tests..."
for projectToTest in ${TESTPROJECTS[@]}
do
    dotnet test "$REPOROOT/test/$projectToTest/project.json" --configuration "$CONFIGURATION" -xml "$projectToTest-testResults.xml"
done
