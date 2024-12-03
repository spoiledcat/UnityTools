#!/bin/bash -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

OS="Mac"
if [[ -e "/c/" ]]; then
  OS="Windows"
fi

CONFIGURATION=Release
PUBLIC=""
BUILD=0
UPM=0
UNITYVERSION=2019.2
CI=0

while (( "$#" )); do
  case "$1" in
    -d|--debug)
      CONFIGURATION="Debug"
    ;;
    -r|--release)
      CONFIGURATION="Release"
    ;;
    -p|--public)
      PUBLIC="-p:PublicRelease=true"
    ;;
    -b|--build)
      BUILD=1
    ;;
    -u|--upm)
      UPM=1
    ;;
    -c)
      shift
      CONFIGURATION=$1
    ;;
    --ci)
      CI=1
    ;;
    --trace)
      { set -x; } 2>/dev/null
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"${APPVEYOR:-}" != x"" ]]; then
  CI=1
fi

if [[ x"${GITHUB_REPOSITORY:-}" != x"" ]]; then
  CI=1
fi

if [[ x"${CI}" == x"0" ]]; then
  pushd $DIR >/dev/null 2>&1
fi

if [[ x"$BUILD" == x"1" ]]; then

  if [[ x"${CI}" == x"0" ]]; then
    dotnet restore
  fi

  dotnet build --no-restore -c $CONFIGURATION $PUBLIC
fi

dotnet test --no-build --no-restore -c $CONFIGURATION $PUBLIC
#dotnet test --no-build --no-restore -c $CONFIGURATION $PUBLIC --logger "trx;LogFileName=dotnet-test-result.trx"
#dotnet test --no-build --no-restore -c $CONFIGURATION $PUBLIC --logger "trx;LogFileName=dotnet-test-result.trx" --logger "html;LogFileName=dotnet-test-result.html"

if [[ x"$UPM" == x"1" ]]; then
  powershell scripts/Test-Upm.ps1 -UnityVersion $UNITYVERSION
fi

if [[ x"${CI}" == x"0" ]]; then
  popd >/dev/null 2>&1
fi
