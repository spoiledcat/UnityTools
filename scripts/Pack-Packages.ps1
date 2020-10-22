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

Get-ChildItem -Directory $srcDir | % {
    if (Test-Path "$srcDir\$($_)\package.json") {
        try {
            Push-Location (Join-Path $srcDir $_.Name)
            $package = Invoke-Command -Fatal { & npm pack -q }
            $package = "$package".Trim()
            $tgt = Join-Path $destdir $package
            Move-Item $package $tgt -Force
            Write-Output "Created package $tgt\$package"
        } finally {
            Pop-Location
        }
    }
}
