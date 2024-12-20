using namespace System.IO
using namespace System.Runtime.InteropServices

[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

$arguments = @(
    'publish'
    '--configuration', $Configuration
    '--verbosity', 'quiet'
    '--runtime', "win-$(([RuntimeInformation]::OSArchitecture).ToString().ToLowerInvariant())"
    '-nologo'
    "-p:Version=1.0.0"
)

$binPath = [Path]::Combine($PSScriptRoot, 'bin')
if (-not (Test-Path -LiteralPath $binPath)) {
    New-Item -Path $binPath -ItemType Directory | Out-Null
}

Get-ChildItem -LiteralPath $PSScriptRoot/src | ForEach-Object -Process {
    Write-Host "Compiling $($_.Name)" -ForegroundColor Cyan

    $csproj = (Get-Item -Path "$([Path]::Combine($_.FullName, '*.csproj'))").FullName
    $outputDir = [Path]::Combine($binPath, $_.Name)
    if (-not (Test-Path -LiteralPath $outputDir)) {
        New-Item -Path $outputDir -ItemType Directory -Force | Out-Null
    }
    Get-ChildItem -LiteralPath $outputDir |
        Where-Object { $_.Extension -in '.exe', '.pdb' } |
        Remove-Item -Force
    dotnet @arguments --output $outputDir $csproj

    if ($LASTEXITCODE) {
        throw "Failed to compiled code for $framework"
    }
}
