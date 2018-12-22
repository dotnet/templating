#!/bin/bash

CWD="$( pwd )"

if ["$DN3BASEDIR" == ""]; then
DN3BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
fi

echo "$DN3BASEDIR"
for proj in `ls -1G $DN3BASEDIR/src`; do
  for item in `ls -1G $DN3BASEDIR/src/$proj | egrep -e "^(bin|obj)$"`; do
    rm -rf "$DN3BASEDIR/src/$proj/$item"
  done
done

echo "Removing ~/.templateengine..."
rm -rf ~/.templateengine/dotnetcli-preview
rm -rf ~/.templateengine/endtoendtestharness
echo "Removing packages from cache..."
rm -rf ~/.nuget/packages/Microsoft.TemplateEngine.*
echo "Done"
