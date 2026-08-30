param(
    [string]$Configuration = "Release"
)

# Lokaler Rider-Build ohne den automatischen Publish-Schritt des Release-Projekts.
# Das Skript baut die Solution und erzeugt keine GitHub-/Release-Artefakte.

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "YacaPluginSwitcher.sln"

if (-not (Test-Path $solution)) {
    throw "Solution nicht gefunden: $solution"
}

Write-Host "Restore..."
dotnet restore $solution

Write-Host "Build $Configuration|x64..."
dotnet build $solution --configuration $Configuration --no-restore --property:Platform=x64 --property:YacaLocalBuild=true

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Lokaler Build erfolgreich abgeschlossen."
