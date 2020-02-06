@echo off
setlocal

SET CONFIGURATION=Release
SET PUBLIC=
set UNITYBUILD=0

:loop
IF NOT "%1"=="" (
  IF "%1"=="-p" (
    SET PUBLIC=-p:PublicRelease=true
  )
  IF "%1"=="--public" (
    SET PUBLIC=-p:PublicRelease=true
  )
  IF "%1"=="-d" (
    SET CONFIGURATION=Debug
  )
  IF "%1"=="--debug" (
    SET CONFIGURATION=Debug
  )
  IF "%1"=="-r" (
    SET CONFIGURATION=Release
  )
  IF "%1"=="--release" (
    SET CONFIGURATION=Release
  )
  IF "%1"=="-n" (
    SET UNITYBUILD=1
  )
  IF "%1"=="--unity" (
    SET UNITYBUILD=1
  )
  IF "%1"=="-c" (
    SET CONFIGURATION=%2
    SHIFT
  )
  SHIFT
  GOTO :loop
)

if "x%UNITYBUILD%" == "x1" (
  set CONFIGURATION=%CONFIGURATION%Unity
)

if "x%APPVEYOR%" == "x" (
  dotnet restore
)

dotnet build --no-restore -c %CONFIGURATION% %PUBLIC%

