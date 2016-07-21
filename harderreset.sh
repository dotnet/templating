#!/bin/bash

CWD="$( pwd )"

if ["$DN3BASEDIR" == ""]; then
DN3BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
fi

for proj in `ls -1G $DN3BASEDIR/src`; do
  for item in `ls -1G $DN3BASEDIR/src/$proj | egrep -e "^(bin|obj)$"`; do
    rm -rf "$DN3BASEDIR/src/$proj/$item"
  done
done

rm -rf "~/.netnew"
rm -rf ~/.nuget/packages/Microsoft.TemplateEngine.*
