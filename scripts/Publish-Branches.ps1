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

$version = $env:CloudBuildNumber
$isPublic = $env:PublicRelease

if (!$version -or $version -eq "") {
    $versionData = & "$($env:USERPROFILE)\.nuget\packages\nerdbank.gitversioning\3.1.91\tools\Get-Version.ps1"
    $version = $versionData.CloudBuildNumber
    $isPublic = $versionData.PublicRelease
}

$srcdir = Join-Path $rootDirectory 'build\packages'

Push-Location $srcdir

try {

    Get-ChildItem | % {
        try {
            Push-Location $_.Name
            $branch = "packages/$($_.Name)"
            if ($isPublic) {
                $branch = "$branch/v$($version)"
            } else {
                $branch = "$branch/latest"
            }
            $msg = "$($_.Name) v$($version)"
            Write-Output "Publishing branch: $branch ($($version))"
            Invoke-Command -Quiet { & git init . }
            Invoke-Command -Quiet { & git remote add origin git@github.com:spoiledcat/UnityTools }
            Invoke-Command -Quiet { & git fetch origin }
            Invoke-Command -Quiet { & git co -fb $branch }
            Invoke-Command -Quiet { & git add . }
            Invoke-Command -Quiet { & git commit -m "$msg" }
            Invoke-Command -Quiet { & git rebase -srecursive -Xours origin/$branch }
            Invoke-Command -Quiet { & git push origin ${branch}:${branch} }
        } finally {
            Pop-Location
        }
    }

} finally {
    Pop-Location
}
