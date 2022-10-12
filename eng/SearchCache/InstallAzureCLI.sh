function retry {
  local n=0
  local max=5
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

retry curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash