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
UNITYBUILD=0

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
    -n|--unity)
      UNITYBUILD=1
    ;;
    -c)
      shift
      CONFIGURATION=$1
    ;;
    --ispublic)
      shift
      if [[ x"$1" == x"1" ]]; then
        PUBLIC="-p:PublicRelease=true"
      fi
    ;;
    -*|--*=) # unsupported flags
      echo "Error: Unsupported flag $1" >&2
      exit 1
    ;;
  esac
  shift
done

if [[ x"$UNITYBUILD" == x"1" ]]; then
  CONFIGURATION="${CONFIGURATION}Unity"
fi

pushd $DIR >/dev/null 2>&1

if [[ x"$BUILD" == x"1" ]]; then

  if [[ x"${APPVEYOR:-}" == x"" ]]; then
    dotnet restore
  fi
  dotnet build --no-restore -c $CONFIGURATION $PUBLIC

fi

dotnet pack --no-build --no-restore -c $CONFIGURATION $PUBLIC

if [[ x"$UPM" == x"1" ]]; then
  powershell scripts/Pack-Upm.ps1
elif [[ x"$UNITYBUILD" == x"0" ]]; then

if [[ x"$OS" == x"Windows" ]]; then
  powershell scripts/Pack-Npm.ps1
else
  srcdir="$DIR/build/packages"
  targetdir="$DIR/build/npm"
  mkdir -p $targetdir
  rm -f $targetdir/*

  cat >$targetdir/packages.json <<EOL
{
EOL

  pushd $srcdir >/dev/null 2>&1
  count=0
  found=0
  for j in `ls -d *`; do
    pushd $j >/dev/null 2>&1
    if [[ -e package.json ]]; then
      tgz="$(npm pack -q)"
      mv -f $tgz $targetdir/$tgz
      cp package.json $targetdir/$tgz.json
      found=1
    fi
    popd >/dev/null 2>&1

    comma=""
    if [[ x"$count" == x"1" ]]; then comma=","; fi

    if [[ x"$found" == x"1" ]];then
      json="$(cat $targetdir/$tgz.json)"
      cat >>$targetdir/packages.json <<EOL
    ${comma}
    "${tgz}": ${json}
EOL

      count=1
    fi

    echo "Created package $targetdir/$tgz"
  done
  popd >/dev/null 2>&1

  cat >>$targetdir/packages.json <<EOL
}
EOL

fi
fi
popd >/dev/null 2>&1
