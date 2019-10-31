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
New-Item -itemtype Directory -Path "npm" -Force -ErrorAction SilentlyContinue

Get-ChildItem | % {
    try {
        Push-Location $_.Name
        Invoke-Command -Fatal { & npm pack }
        Move-Item *.tgz ..\..\npm
    } finally {
        Pop-Location
    }
}
Pop-Location
