[CmdletBinding()]
param(
    [int]$TargetPid = 0
)

. (Join-Path $PSScriptRoot '_common.ps1')

Test-Tool dotnet-gcdump

$proc = Get-HermesPid -TargetPid $TargetPid
$outDir = New-OutputDir
$dump = Join-Path $outDir 'trace.gcdump'
$report = Join-Path $outDir 'trace.gcdump.txt'

Write-Host "[hermes-profile heap] pid=$proc"
Write-Host "[hermes-profile heap] -> $dump"

& dotnet-gcdump collect -p $proc -o $dump
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet-gcdump collect failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $dump)) {
    Write-Error "Expected gcdump at $dump was not produced."
    exit 9
}

Write-Host '[hermes-profile heap] Generating text report...'
& dotnet-gcdump report $dump | Out-File -FilePath $report -Encoding utf8

$dumpSize = (Get-Item -LiteralPath $dump).Length
$reportSize = if (Test-Path -LiteralPath $report) { (Get-Item -LiteralPath $report).Length } else { 0 }
Write-Host '[hermes-profile heap] Done.' -ForegroundColor Green
Write-Host ("  gcdump : {0} ({1:N0} bytes)" -f $dump, $dumpSize)
Write-Host ("  report : {0} ({1:N0} bytes)" -f $report, $reportSize)
Write-Host ''
Write-Host 'Open the .gcdump in Visual Studio or PerfView for retainer / dominator analysis.'
