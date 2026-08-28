[CmdletBinding()]
param()
$ErrorActionPreference='Stop';$Root=Split-Path -Parent $PSScriptRoot
$service=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/YacaService.cs')-Raw
if($service-notmatch 'AddCandidate\s*\(\s*candidates\s*,\s*TargetFile\s*\)'-or$service-notmatch 'AddCandidate\s*\(\s*candidates\s*,\s*Settings\.TeamSpeakPluginDirectory\s*\)'-or$service-notmatch 'Settings\.TeamSpeakPluginDirectories'){throw 'Installed YACA detection must inspect configured TeamSpeak directories.'}
Write-Host 'Installed-path preflight: PASS' -ForegroundColor Green
