[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Filter,

    [string[]]$ExtraArgs
)

. (Join-Path $PSScriptRoot '_common.ps1')

$root = Get-ProjectRoot
$benchProj = Join-Path $root 'HermesProxy.Benchmarks\HermesProxy.Benchmarks.csproj'

if (-not (Test-Path -LiteralPath $benchProj)) {
    Write-Error "HermesProxy.Benchmarks project not found at $benchProj"
    exit 11
}

$dotnetArgs = @(
    'run',
    '--project', $benchProj,
    '-c', 'Release'
)
$appArgs = @('--filter', $Filter, '--exporters', 'json')
if ($ExtraArgs) { $appArgs += $ExtraArgs }

Write-Host "[hermes-profile bench] filter=$Filter"
Write-Host "[hermes-profile bench] cmd: dotnet $($dotnetArgs -join ' ') -- $($appArgs -join ' ')"
Write-Host ''

Push-Location $root
try {
    & dotnet @dotnetArgs '--' @appArgs
    $exit = $LASTEXITCODE
}
finally {
    Pop-Location
}

if ($exit -eq 0) {
    $resultsDir = Join-Path $root 'BenchmarkDotNet.Artifacts\results'
    if (Test-Path -LiteralPath $resultsDir) {
        Write-Host ''
        Write-Host '[hermes-profile bench] Latest result files:' -ForegroundColor Green
        Get-ChildItem -LiteralPath $resultsDir -File |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 6 |
            ForEach-Object { Write-Host ("  {0}  {1}" -f $_.LastWriteTime.ToString('HH:mm:ss'), $_.FullName) }
    }
}

exit $exit
