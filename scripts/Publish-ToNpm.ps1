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

Push-Location "build\npm"
Get-ChildItem *.tgz | % {
    try {
        Invoke-Command -Fatal { & npm publish $_.Name }
    } finally {
        Pop-Location
    }
}
Pop-Location
