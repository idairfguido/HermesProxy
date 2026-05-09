[CmdletBinding()]
param(
    [int]$Seconds = 60,
    [int]$TargetPid = 0,
    [string]$Counters = 'System.Runtime,System.Net.Sockets'
)

. (Join-Path $PSScriptRoot '_common.ps1')

Test-Tool dotnet-counters

$proc = Get-HermesPid -TargetPid $TargetPid
$outDir = New-OutputDir
$csv = Join-Path $outDir 'counters.csv'

Write-Host "[hermes-profile counters] pid=$proc duration=${Seconds}s counters=$Counters"
Write-Host "[hermes-profile counters] -> $csv"

$collectArgs = @(
    'collect',
    '-p', $proc,
    '--counters', $Counters,
    '--refresh-interval', 1,
    '--format', 'csv',
    '-o', $csv
)

$proc_obj = Start-Process -FilePath 'dotnet-counters' -ArgumentList $collectArgs -PassThru -NoNewWindow
try {
    Start-Sleep -Seconds $Seconds
} finally {
    if (-not $proc_obj.HasExited) {
        Write-Host '[hermes-profile counters] Stopping collection...'
        try { $proc_obj.CloseMainWindow() | Out-Null } catch {}
        Start-Sleep -Milliseconds 500
        if (-not $proc_obj.HasExited) {
            try { Stop-Process -Id $proc_obj.Id -Force -ErrorAction SilentlyContinue } catch {}
        }
    }
}

if (-not (Test-Path -LiteralPath $csv)) {
    Write-Error "Expected CSV at $csv was not produced. dotnet-counters may need a longer run window."
    exit 8
}

$size = (Get-Item -LiteralPath $csv).Length
Write-Host '[hermes-profile counters] Done.' -ForegroundColor Green
Write-Host ("  csv : {0} ({1:N0} bytes)" -f $csv, $size)
Write-Host ''
Write-Host 'Look for: alloc-rate, gen-2-gc-count, monitor-lock-contention-count, threadpool-thread-count, exception-count.'
