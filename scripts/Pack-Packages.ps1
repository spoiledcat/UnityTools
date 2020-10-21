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

$destdir = Join-Path $rootDirectory 'build\npm'
$srcdir = Join-Path $rootDirectory 'build\packages'

New-Item -itemtype Directory -Path $destdir -Force -ErrorAction SilentlyContinue

Push-Location $srcdir

try {

    Get-ChildItem | % {
        try {
            Push-Location $_.Name
            Write-Output "Packing $($_.Name)"
            Invoke-Command -Fatal -Quiet { & npm pack }
            Move-Item *.tgz $destdir
        } finally {
            Pop-Location
        }
    }

} finally {
    Pop-Location
}
