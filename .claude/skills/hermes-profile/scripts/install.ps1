[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$tools = @('dotnet-trace', 'dotnet-counters', 'dotnet-gcdump', 'dotnet-dump')

Write-Host '[hermes-profile install] Checking installed dotnet global tools...'

$installed = @()
try {
    $listOutput = & dotnet tool list -g 2>$null
    foreach ($line in $listOutput) {
        if ($line -match '^\s*(\S+)\s') { $installed += $Matches[1] }
    }
} catch {
    Write-Host '[hermes-profile install] dotnet tool list -g failed; assuming nothing installed.' -ForegroundColor Yellow
}

$missing = $tools | Where-Object { $installed -notcontains $_ }

if (-not $missing) {
    Write-Host '[hermes-profile install] All four diagnostics tools already installed:' -ForegroundColor Green
    $tools | ForEach-Object { Write-Host "  $_" }
    Write-Host ''
    Write-Host 'PATH check:'
    foreach ($t in $tools) {
        $cmd = Get-Command $t -ErrorAction SilentlyContinue
        if ($cmd) { Write-Host "  $t -> $($cmd.Source)" } else { Write-Host "  $t -> NOT ON PATH (try: `$env:PATH += ';' + `"`$HOME\.dotnet\tools`")" -ForegroundColor Yellow }
    }
    exit 0
}

foreach ($t in $missing) {
    Write-Host "[hermes-profile install] Installing $t..."
    & dotnet tool install --global $t
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet tool install --global $t failed (exit $LASTEXITCODE)."
        exit $LASTEXITCODE
    }
}

Write-Host '[hermes-profile install] Done.' -ForegroundColor Green
Write-Host 'If `dotnet-trace` is still not found, ensure $HOME\.dotnet\tools is on PATH and reopen this shell.'
