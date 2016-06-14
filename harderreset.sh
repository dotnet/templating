#!/bin/bash

CWD="$( pwd )"

if ["$DN3BASEDIR" == ""]; then
DN3BASEDIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
fi

for proj in `dir -1G $DN3BASEDIR/src`; do
  for item in `dir -1G $DN3BASEDIR/src/$proj | grep -P "^(bin|obj)$"`; do
    rm -rf "$DN3BASEDIR/src/$proj/$item"
  done
done

rm -rf "~/.netnew"
rm -rf ~/.nuget/packages/Microsoft.TemplateEngine.*
