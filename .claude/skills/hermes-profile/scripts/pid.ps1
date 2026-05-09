[CmdletBinding()]
param()

. (Join-Path $PSScriptRoot '_common.ps1')

Test-Tool dotnet-trace

$proc = Get-HermesPid
Write-Host $proc
