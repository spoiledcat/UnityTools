#!/bin/sh -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

CONFIGURATION=""

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
      shift
    ;;
    -r|--release)
      CONFIGURATION="Release"
      shift
    ;;
    -p|--public)
      shift
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
      ;;
    *) # preserve positional arguments
      if [[ x"$CONFIGURATION" != x"" ]]; then
        echo "Invalid argument $1"
        exit -1
      fi
      CONFIGURATION="$1"
      shift
      ;;
  esac
done

if [[ x"$CONFIGURATION" == x"" ]]; then
  CONFIGURATION="Debug"
fi


pushd $DIR
dotnet pack --no-build --no-restore -c $CONFIGURATION
popd