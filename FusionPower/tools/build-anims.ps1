param([string]$Kanimal = 'kanimal-cli.exe', [string]$Only)

& (Join-Path $PSScriptRoot 'build-anims-v2.ps1') -Kanimal $Kanimal -Only $Only
if ($LASTEXITCODE -ne 0) { throw 'KAnim conversion failed.' }
