[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    # Placeholder: T12 will add test execution here.
    [switch]$Test
)

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot

$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path -LiteralPath $vswhere) {
    $msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
}

if (-not $msbuild -or -not (Test-Path -LiteralPath $msbuild)) {
    Write-Host 'ERROR: MSBuild was not found.' -ForegroundColor Red
    Write-Host 'Install "Visual Studio 2022 Build Tools" (workload: .NET desktop build tools / MSBuild)'
    Write-Host 'and the ".NET Framework 4.7.2 Developer Pack", then re-run this script.'
    exit 2
}

Write-Host "MSBuild: $msbuild"

& $msbuild 'RealVisionLab.sln' -t:restore -p:RestorePackagesConfig=true -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $msbuild 'RealVisionLab.sln' "-p:Configuration=$Configuration" -m -nologo -v:minimal
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Test) {
    Write-Host 'Test: not implemented yet (placeholder for T12).'
}
exit 0
