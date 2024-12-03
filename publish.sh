#!/bin/bash -eu
{ set +x; } 2>/dev/null
SOURCE="${BASH_SOURCE[0]}"
DIR="$( cd -P "$( dirname "$SOURCE" )" >/dev/null 2>&1 && pwd )"

OS="Mac"
if [[ -e "/c/" ]]; then
  OS="Windows"
fi

PUBLIC=""
NPM=0
UNITYVERSION=2019.2
YAMATO=0
BRANCHES=0
NUGET=0
VERSION=
PUBLIC=0
CI=0
SKIP_CLONE=0
LATEST=0

while (( "$#" )); do
  case "$1" in
    -p|--public)
      PUBLIC=1
    ;;
    -u|--npm)
      NPM=1
    ;;
    -c|--branches)
      BRANCHES=1
    ;;
    -g|--nuget)
      NUGET=1
    ;;
    -g|--github)
      GITHUB=1
    ;;
    -v|--version)
      shift
      VERSION=$1
    ;;
    --ispublic)
      shift
      PUBLIC=$1
    ;;
    --latest)
      shift
      LATEST=$1
    ;;
    --ci)
      CI=1
    ;;
    --skip-clone)
      SKIP_CLONE=1
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

if [[ x"${YAMATO_JOB_ID:-}" != x"" ]]; then
  CI=1
  YAMATO=1
  export GITLAB_CI=1
  export CI_COMMIT_TAG="${GIT_TAG:-}"
  export CI_COMMIT_REF_NAME="${GIT_BRANCH:-}"
fi

if [[ x"$VERSION" == x"" ]]; then
  VERSION=${NBGV_NpmPackageVersion:-}
fi  

function updateBranchAndPush() {
  local branch=$1
  local destdir=$2
  local pkgdir=$3
  local msg=$4
  local ver=$5
  local publ=$6

  echo "Publishing branch: $branch/latest ($VERSION)"

  pushd $destdir

  git reset --hard 40c898effcd16bc648ddd57
  git clean -xdf
  git reset --hard origin/$branch/latest >/dev/null 2>&1||true
  rm -rf *
  cp -R $pkgdir/* .
  git add .
  git commit -m "$msg"

  if [[ x"${LATEST}" == x"1" ]]; then
    git push origin HEAD:refs/heads/$branch/latest
  fi

  if [[ $publ -eq 1 ]]; then
      echo "Publishing branch: $branch/$VERSION"
      git push origin +HEAD:refs/heads/$branch/$VERSION
  fi

  popd
}

if [[ x"$BRANCHES" == x"1" ]]; then

  if [[ x"$VERSION" == x"" ]]; then
    dotnet tool install -v q --tool-path . nbgv || true
    VERSION=$(./nbgv cloud -s VisualStudioTeamServices --all-vars -p src|grep NBGV_CloudBuildNumber|cut -d']' -f2)
    _public=$(./nbgv cloud -s VisualStudioTeamServices --all-vars -p src|grep NBGV_PublicRelease|cut -d']' -f2)
    if [[ x"${_public}" == x"True" ]]; then
      PUBLIC=1
    fi
  fi

  srcdir=$DIR/build/packages
  destdir=$( cd .. >/dev/null 2>&1 && pwd )/branches

  if [[ x"${SKIP_CLONE}" == "0" ]]; then
    test -d $destdir && rm -rf $destdir
    mkdir -p $destdir
    git clone -q --branch=empty git@github.com:spoiledcat/UnityTools $destdir
  fi

  pushd $srcdir

  for name in *;do
    test -f $name/package.json || continue
    branch=packages/$name
    msg="$name v$VERSION"
    pkgdir=$srcdir/$name

    updateBranchAndPush "$branch" "$destdir" "$pkgdir" "$msg" "$VERSION" $PUBLIC
  done

  popd

fi

if [[ x"$NUGET" == x"1" ]]; then

  if [[ x"${PUBLISH_KEY:-}" == x"" ]]; then
    echo "Can't publish without a PUBLISH_KEY environment variable in the user:token format" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  if [[ x"${PUBLISH_URL:-}" == x"" ]]; then
    echo "Can't publish without a PUBLISH_URL environment variable" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  for p in "$DIR/build/nuget/**/*nupkg"; do
    dotnet nuget push $p -ApiKey "${PUBLISH_KEY}" -Source "${PUBLISH_URL}"
  done

fi

if [[ x"$NPM" == x"1" ]]; then

  #if in ci, only publish if public or in master
  if [[ x"${CI}" == x"1" ]]; then
    if [[ x"$PUBLIC" != x"1" ]]; then

      if [[ x"${APPVEYOR:-}" != x"" ]]; then
        if [[ x"${APPVEYOR_PULL_REQUEST_NUMBER:-}" != x"" ]]; then
          echo "Skipping publishing non-public packages in CI on pull request builds"
          exit 0
        fi
        if [[ x"${APPVEYOR_REPO_BRANCH:-}" != x"master" ]]; then
          echo "Skipping publishing non-public packages in CI on pushes to branches other than master"
          exit 0
        fi
      fi

      if [[ x"${GITHUB_REPOSITORY:-}" != x"" ]]; then
        if [[ x"${GITHUB_REF:-}" != x"refs/heads/master" ]]; then
          echo "Skipping publishing non-public packages in CI on pushes to branches other than master"
          exit 0
        fi
      fi
    fi
  fi

  if [[ x"${NPM_TOKEN:-}" == x"" ]]; then
    echo "Can't publish without a NPM_TOKEN environment variable" >&2
    popd >/dev/null 2>&1
    exit 1
  fi

  npm config set registry https://registry.spoiledcat.com
  npm config set //registry.spoiledcat.com/:_authToken $NPM_TOKEN
  pushd build/npm
  for pkg in *.tgz;do
    npm publish -quiet $pkg
  done
  popd
fi
