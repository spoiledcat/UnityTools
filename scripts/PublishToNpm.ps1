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
    try {
            Push-Location $_.Name
            Invoke-Command -Fatal { & npm publish --dry-run }
        }
    } finally {
        Pop-Location
    }
}
Pop-Location
