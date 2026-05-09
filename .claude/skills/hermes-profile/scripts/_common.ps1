$ErrorActionPreference = 'Stop'

function Get-ProjectRoot {
    $root = & git -C $PSScriptRoot rev-parse --show-toplevel 2>$null
    if (-not $root) {
        return (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))))
    }
    return $root.Trim()
}

function Get-HermesPid {
    param([int]$TargetPid = 0)

    if ($TargetPid -gt 0) { return $TargetPid }

    $tool = Get-Command dotnet-trace -ErrorAction SilentlyContinue
    if (-not $tool) {
        Write-Error 'dotnet-trace is not installed or not on PATH. Run "/hermes-profile install" first.'
        exit 4
    }

    $lines = & dotnet-trace ps 2>$null
    if (-not $lines) {
        Write-Error 'dotnet-trace ps returned no output — no .NET processes are running.'
        exit 4
    }

    $found = @()
    foreach ($line in $lines) {
        $trim = $line.Trim()
        if (-not $trim) { continue }
        if ($trim -notmatch '^\s*(\d+)\s+(\S+)\s+(.*)$') { continue }
        $procPid = [int]$Matches[1]
        $procName = $Matches[2]
        $cmd = $Matches[3]

        # Only accept .NET host or the proxy itself; skip shells/IDEs whose cwd happens
        # to be inside a folder named HermesProxy.
        $nameOk = $procName -match '^(dotnet|HermesProxy)(\.exe)?$'
        if (-not $nameOk) { continue }

        # And require the actual entry assembly/executable on the command line.
        $cmdOk = $cmd -match '(?i)[\\/\s"]HermesProxy\.(dll|exe)(\s|"|$)'
        if (-not $cmdOk) { continue }

        $found += [pscustomobject]@{ ProcessId = $procPid; Name = $procName; Cmd = $cmd }
    }

    if ($found.Count -eq 0) {
        Write-Error 'No HermesProxy process found. Start one with "/hermes-run release" first.'
        exit 5
    }
    if ($found.Count -gt 1) {
        Write-Host '[hermes-profile] Multiple HermesProxy candidates found:' -ForegroundColor Yellow
        foreach ($m in $found) { Write-Host ("  pid={0}  name={1}  cmd={2}" -f $m.ProcessId, $m.Name, $m.Cmd) }
        Write-Error 'Pass -TargetPid <int> explicitly to disambiguate.'
        exit 6
    }

    return [int]$found[0].ProcessId
}

function New-OutputDir {
    $root = Get-ProjectRoot
    $stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMdd_HHmmss')
    $dir = Join-Path $root ("artifacts\profile\" + $stamp)
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    return $dir
}

function Test-Tool {
    param([Parameter(Mandatory)][string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Error "$Name is not installed or not on PATH. Run '/hermes-profile install' first."
        exit 4
    }
}
