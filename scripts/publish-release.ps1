param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Project = Join-Path $Root "src\YacaPluginSwitcher\YacaPluginSwitcher.csproj"
$ReleaseDir = Join-Path $Root "releases"
$Version = ([xml](Get-Content (Join-Path $Root "Directory.Build.props") -Raw)).Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1
$VersionValue = if ($Version.Version) { $Version.Version } else { "unknown" }

Write-Host "Publishing YacaPluginSwitcher .NET 10 win-x64..." -ForegroundColor Cyan

if ($Configuration -eq "Release" -and $Version) {
    if ($Version -notmatch "^\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?$") { throw "Invalid release version: $Version" }
    dotnet publish $Project -c $Configuration -r win-x64 --self-contained true -p:Version=$Version -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:YacaReleasePackaging=true
} else {
    dotnet publish $Project -c $Configuration -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=false -p:YacaReleasePackaging=true
}

if ($Configuration -eq "Release") {
    $zip = Get-ChildItem $ReleaseDir -Filter "YacaPluginSwitcher-v*-win-x64.zip" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $zip) { throw "No release ZIP was generated in $ReleaseDir." }

    $shaPath = "$($zip.FullName).sha256"
    $hash = (Get-FileHash -LiteralPath $zip.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $($zip.Name)" | Set-Content -LiteralPath $shaPath -Encoding ascii

    $zipItems = & tar -tf $zip.FullName
    $files = @($zipItems | Where-Object { $_ -and -not $_.EndsWith('/') })
    $exeFiles = @($files | Where-Object { $_ -match '(?i)\.exe$' })
    $forbidden = @($files | Where-Object { $_ -match '(?i)\.(pdb|dll|deps\.json|runtimeconfig\.json)$' })
    if ($exeFiles.Count -ne 1) { throw "Release ZIP must contain exactly one EXE; found $($exeFiles.Count)." }
    if ($forbidden.Count -ne 0) { throw "Release ZIP contains forbidden runtime/debug files: $($forbidden -join ', ')" }

    $stage = Join-Path $ReleaseDir ([IO.Path]::GetFileNameWithoutExtension($zip.Name))
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }

    Write-Host "Release: $($zip.Name)" -ForegroundColor Green
    Write-Host "SHA-256: $hash" -ForegroundColor Green
    Write-Host "Output:  $ReleaseDir" -ForegroundColor Green
}
