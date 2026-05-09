[CmdletBinding()]
param(
    [int]$Seconds = 30,
    [switch]$Detailed,
    [int]$TargetPid = 0
)

. (Join-Path $PSScriptRoot '_common.ps1')

Test-Tool dotnet-trace

$proc = Get-HermesPid -TargetPid $TargetPid
$outDir = New-OutputDir
$nettrace = Join-Path $outDir 'trace.nettrace'
$speedscope = Join-Path $outDir 'trace.speedscope.json'

$durationStr = [TimeSpan]::FromSeconds($Seconds).ToString('hh\:mm\:ss')

$collectArgs = @(
    'collect',
    '-p', $proc,
    '--duration', $durationStr,
    '-o', $nettrace
)
if ($Detailed) {
    $collectArgs += @('--providers', 'Microsoft-Windows-DotNETRuntime:0x4c14fccbd:5')
} else {
    $collectArgs += @('--profile', 'cpu-sampling')
}

Write-Host "[hermes-profile cpu] pid=$proc duration=$durationStr profile=$(if($Detailed){'detailed'}else{'cpu-sampling'})"
Write-Host "[hermes-profile cpu] -> $nettrace"
& dotnet-trace @collectArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet-trace collect failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $nettrace)) {
    Write-Error "Expected trace at $nettrace was not produced."
    exit 7
}

Write-Host '[hermes-profile cpu] Converting to speedscope JSON...'
& dotnet-trace convert --format speedscope -o $speedscope $nettrace
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet-trace convert failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

$nettraceSize = (Get-Item -LiteralPath $nettrace).Length
$ssSize = if (Test-Path -LiteralPath $speedscope) { (Get-Item -LiteralPath $speedscope).Length } else { 0 }
Write-Host '[hermes-profile cpu] Done.' -ForegroundColor Green
Write-Host ("  nettrace   : {0} ({1:N0} bytes)" -f $nettrace, $nettraceSize)
Write-Host ("  speedscope : {0} ({1:N0} bytes)" -f $speedscope, $ssSize)
Write-Host ''
Write-Host 'Open the speedscope.json at https://www.speedscope.app (drag-drop, all client-side).'
