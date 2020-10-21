[CmdletBinding()]
Param(
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

$version = $env:GitAssemblyInformationalVersion
$isPublic = $env:NBGV_PublicRelease

if (!$version -or $version -eq "") {
    $versionData = & "$($env:USERPROFILE)\.nuget\packages\nerdbank.gitversioning\3.1.91\tools\Get-Version.ps1"
    $version = $versionData.CloudBuildNumber
    $isPublic = $versionData.PublicRelease
}

$packagesDir = Join-Path $rootDirectory 'build\packages'
$gitDir = Split-Path $rootDirectory
$gitDir = Join-Path $gitDir 'branches'

New-Item -itemtype Directory -Path $gitDir -Force -ErrorAction SilentlyContinue

Invoke-Command -Quiet { & git clone -q --branch=empty git@github.com:spoiledcat/UnityTools $gitDir }

Push-Location $packagesDir

try {

    Get-ChildItem | % {
        try {

            $branch = "packages/$($_.Name)"
            if ($isPublic) {
                $branch = "$branch/v$($version)"
            } else {
                $branch = "$branch/latest"
            }
            $msg = "$($_.Name) v$($version)"
            Write-Output "Publishing branch: $branch ($($version))"

            $srcDir = Join-Path $packagesDir $_.Name
          
            Push-Location $gitDir

            Invoke-Command -Quiet { & git reset --hard 40c898effcd16bc648ddd57 }
            Invoke-Command -Quiet { & git reset --hard origin/$branch }
            Remove-Item "$gitDir\*" -Exclude ".git\" -Recurse
            Copy-Item "$srcDir\*" $gitDir -Force -Recurse
            Invoke-Command -Quiet { & git add . }
            Invoke-Command -Quiet { & git commit -m "$msg" }
            Invoke-Command -Quiet { & git push origin HEAD:${branch} }
            Pop-Location
        } finally {
            Pop-Location
        }
    }

} finally {
    Pop-Location
}
