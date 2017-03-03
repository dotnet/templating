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
DDIR="$REPOROOT/dev"
export CONFIGURATION="Debug"

PROJECTSTOPACK=( \
    Microsoft.TemplateEngine.Utils \
    Microsoft.TemplateEngine.Core.Contracts \
    Microsoft.TemplateEngine.Core \
    Microsoft.TemplateEngine.Edge \
    Microsoft.TemplateEngine.Abstractions \
    Microsoft.TemplateEngine.Orchestrator.RunnableProjects \
    Microsoft.TemplateEngine.Cli
 )

PROJECTFRAMEWORKS=( \
    netstandard1.3 \
    netstandard1.3 \
    netstandard1.3 \
    netcoreapp1.1 \
    netstandard1.3 \
    netstandard1.3 \
    netcoreapp1.1 
 )

TESTPROJECTS=( \
    Microsoft.TemplateEngine.Core.UnitTests \
    Microsoft.TemplateEngine.Utils.UnitTests
)

TESTFRAMEWORK="netcoreapp1.1"

source "$REPOROOT/scripts/common/_prettyprint.sh"

while [[ $# > 0 ]]; do
    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -c|--configuration)
            export CONFIGURATION=$2
            shift
            ;;
        -r|--runtime)
            export RID=$2
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
for projectToTest in ${PROJECTSTOPACK[@]}
do
    $DOTNET_INSTALL_DIR/dotnet restore "$REPOROOT/src/$projectToTest/$projectToTest.csproj"
done

echo "Restoring all test projects..."
for projectToTest in ${TESTPROJECTS[@]}
do
    dotnet restore "$REPOROOT/test/$projectToTest/$projectToTest.csproj"
done

length=${#PROJECTSTOPACK[@]}
for (( i=0; i<${length} ; i++ ));
do
    projectToPack=${PROJECTSTOPACK[$i]}
    framework=${PROJECTFRAMEWORKS[$i]}
    echo "Build $projectToPack..."
    dotnet msbuild "$REPOROOT/src/$projectToPack/$projectToPack.csproj" /p:"Configuration=$CONFIGURATION;TargetFramework=$framework"
done

for (( i=0; i<${length} ; i++ ));
do
    projectToPack=${PROJECTSTOPACK[$i]}
    framework=${PROJECTFRAMEWORKS[$i]}
    dotnet pack "$REPOROOT/src/$projectToPack/$projectToPack.csproj" --output "$PACKAGESDIR" --configuration "$CONFIGURATION" --no-build /p:TargetFramework=$framework
done

echo "Build dotnet new3..."
THISDIR=$(pwd)
cd $REPOROOT/src/dotnet-new3
dotnet msbuild "$REPOROOT/src/dotnet-new3/dotnet-new3.csproj" /t:Restore /p:RuntimeIdentifier=$RID /p:TargetFramework=netcoreapp1.1 /p:RestoreRecursive=False /p:RestoreSources="https:%2F%2Fapi.nuget.org%2Fv3%2Findex.json;$REPOROOT/artifacts"
dotnet publish "$REPOROOT/src/dotnet-new3/dotnet-new3.csproj" -c $CONFIGURATION -f netcoreapp1.1 -o "$DDIR" -r $RID
cd $THISDIR

echo "Running tests..."
length=${#TESTPROJECTS[@]}
for (( i=0; i<${length} ; i++ ));
do
    projectToPack=${TESTPROJECTS[$i]}
    dotnet test "$REPOROOT/test/$projectToTest/$projectToTest.csproj" /p:"Configuration=$CONFIGURATION;TargetFramework=$TESTFRAMEWORK"
done
