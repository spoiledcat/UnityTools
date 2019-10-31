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

Get-ChildItem $destdir | % {
    Invoke-Command { & npm publish "$destdir\$($_.Name)" }
}
