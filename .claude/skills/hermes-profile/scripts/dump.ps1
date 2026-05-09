[CmdletBinding()]
param(
    [int]$TargetPid = 0,
    [ValidateSet('Full', 'Heap', 'Mini', 'Triage')]
    [string]$Type = 'Heap'
)

. (Join-Path $PSScriptRoot '_common.ps1')

Test-Tool dotnet-dump

$proc = Get-HermesPid -TargetPid $TargetPid
$outDir = New-OutputDir
$dmp = Join-Path $outDir 'trace.dmp'

Write-Host "[hermes-profile dump] pid=$proc type=$Type"
Write-Host "[hermes-profile dump] -> $dmp"

& dotnet-dump collect -p $proc --type $Type -o $dmp
if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet-dump collect failed (exit $LASTEXITCODE)."
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $dmp)) {
    Write-Error "Expected dump at $dmp was not produced."
    exit 10
}

$size = (Get-Item -LiteralPath $dmp).Length
Write-Host '[hermes-profile dump] Done.' -ForegroundColor Green
Write-Host ("  dmp : {0} ({1:N0} bytes / {2:N1} MB)" -f $dmp, $size, ($size / 1MB))
Write-Host ''
Write-Host "Analyse with:"
Write-Host "  dotnet-dump analyze `"$dmp`""
Write-Host "Then SOS commands like: clrstack -all, dumpheap -stat, gcroot <addr>, parallelstacks, syncblk."
