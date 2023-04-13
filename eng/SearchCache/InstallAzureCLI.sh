#!/usr/bin/env bash

function retry {
  local n=0
  local max=15
  local delay=60
  while true; do
    ((n++))
    "$@" && {
      echo "Command succeeded."
      break
    } || {
      if [[ $n -lt $max ]]; then
        echo "Command failed with the attempt $n/$max."
        sleep $delay;
      else
        echo "The command has failed after $n attempts."
        break
      fi
    }
  done
}

install_script="/tmp/azurecli_install.sh"
curl -sL https://aka.ms/InstallAzureCLIDeb -o "$install_script"
retry sudo bash "$install_script"
