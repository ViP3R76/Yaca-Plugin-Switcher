[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$Root=Split-Path -Parent $PSScriptRoot

# Preflight is intentionally limited to repository/project structure.
# Concrete C#, XAML, resources and assets are validated by dotnet build/runtime.
$required=@('YacaPluginSwitcher.sln','src/YacaPluginSwitcher.Core/YacaPluginSwitcher.Core.csproj','src/YacaPluginSwitcher/YacaPluginSwitcher.csproj','src/YacaPluginSwitcher/App.xaml','src/YacaPluginSwitcher/MainWindow.xaml','src/YacaPluginSwitcher/MainWindow.xaml.cs','src/YacaPluginSwitcher/Styles/Colors.xaml','src/YacaPluginSwitcher/Styles/Controls.xaml','src/YacaPluginSwitcher/Views/BackupView.xaml','src/YacaPluginSwitcher/Views/ConfigView.xaml','src/YacaPluginSwitcher/Views/InfoView.xaml')
foreach($r in $required){if(-not(Test-Path (Join-Path $Root $r) -PathType Leaf)){throw "Required file missing: $r"}}
foreach($p in Get-ChildItem $Root -Recurse -File -Include *.csproj,*.props,*.pubxml){try{[xml](Get-Content $p.FullName -Raw)|Out-Null}catch{throw "Invalid XML: $($p.FullName): $($_.Exception.Message)"}}
foreach($f in Get-ChildItem (Join-Path $Root 'src') -Recurse -File -Include *.cs,*.xaml){$t=Get-Content $f.FullName -Raw;if($t-match '(?i)System\.Windows\.Forms|UseWindowsForms|WinForms|WindowsForms'){throw "Legacy WinForms reference remains: $($f.FullName)"}}
$project=[xml](Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/YacaPluginSwitcher.csproj') -Raw)
if($project.Project.PropertyGroup.TargetFramework -notcontains 'net10.0-windows'){throw 'WPF target framework missing.'}
if($project.Project.PropertyGroup.UseWPF -notcontains 'true'){throw 'UseWPF is not enabled.'}
$main=Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/MainWindow.xaml') -Raw
if($main -notmatch 'WindowChrome'){throw 'WPF WindowChrome missing.'}
if($main -notmatch 'PageHost'){throw 'WPF page host missing.'}
if($main -notmatch 'NavPanel'){throw 'WPF navigation host missing.'}
$app=Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/App.xaml') -Raw
if($app -notmatch 'Colors.xaml' -or $app -notmatch 'Controls.xaml'){throw 'WPF resource dictionaries missing.'}
$appPaths=Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/AppPaths.cs') -Raw
if($appPaths-match 'LocalApplicationData|ApplicationData'){throw 'Non-portable application data path remains.'}
Write-Host 'Preflight PASS - WPF structural validation only.' -ForegroundColor Green
Write-Host 'C#, XAML, references, resources, assets and analyzers are validated by dotnet build.' -ForegroundColor Cyan