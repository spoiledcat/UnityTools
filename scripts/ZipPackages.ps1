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

Push-Location "build\packages"
Get-ChildItem | % {
    Invoke-Command -Fatal { & 7z a "..\$($_.Name).zip" "$($_.Name)" }
}
Pop-Location
