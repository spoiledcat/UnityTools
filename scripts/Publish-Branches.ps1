[CmdletBinding()]
Param(
    [string]
    $Version = ""
    ,
    [bool]
    $IsPublic = $false
    ,
    [switch]
    $IsCI = $false
    ,
    [switch]
    $Trace = $false
)

Set-StrictMode -Version Latest
if ($Trace) {
    Set-PSDebug -Trace 1
}

. $PSScriptRoot\helpers.ps1 | out-null

if ($Version -eq "") {
    Invoke-Command -Quiet { & dotnet tool install -v q --tool-path . nbgv }
    $arr = Invoke-Command { & nbgv cloud -s VisualStudioTeamServices --all-vars -p src }
    $arr = $arr.split("`r`n")

    foreach ($i in 0..($arr.length-1)) {
        $str = $arr[$i].split(";]")
        if ($str.length -eq 3) {
            $strname = $str[0].Replace("##vso[task.setvariable variable=", "")
            $strval = $str[2]
            if ($strname -eq "NBGV_CloudBuildNumber") {
                $Version = $strval
            } elseif ($strname -eq "NBGV_PublicRelease") {
                $IsPublic = $strval -eq "True"
            }
        }
    }
}

$srcDir = Join-Path $rootDirectory 'build\packages'
$destdir = Join-Path (Split-Path $rootDirectory) 'branches'

New-Item -itemtype Directory -Path $destdir -Force -ErrorAction SilentlyContinue

Invoke-Command -Quiet { & git clone -q --branch=empty git@github.com:spoiledcat/UnityTools $destdir }

Get-ChildItem -Directory $srcDir | % {
    if (Test-Path "$srcDir\$($_)\package.json") {
        $branch = "packages/$($_.Name)"
        if ($isPublic) {
            $branch = "$branch/v$($version)"
        } else {
            $branch = "$branch/latest"
        }
        $msg = "$($_.Name) v$($version)"
        $packageDir = Join-Path $srcDir $_.Name

        Write-Output "Publishing branch: $branch ($($version))"
      
        try {

            Push-Location $destdir

            Invoke-Command -Quiet { & git reset --hard 40c898effcd16bc648ddd57 }
            Invoke-Command -Quiet { & git reset --hard origin/$branch }
            Remove-Item "$destdir\*" -Exclude ".git\" -Recurse
            Copy-Item "$packageDir\*" $destdir -Force -Recurse
            Invoke-Command -Quiet { & git add . }
            Invoke-Command -Quiet { & git commit -m "$msg" }
            Invoke-Command -Quiet { & git push origin HEAD:${branch} }

            Pop-Location
        } finally {
            Pop-Location
        }
    }
}