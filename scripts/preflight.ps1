[CmdletBinding()]
param()
$ErrorActionPreference='Stop';$Root=Split-Path -Parent $PSScriptRoot
$required=@('YacaPluginSwitcher.sln','Directory.Build.props','src/YacaPluginSwitcher.Core/YacaPluginSwitcher.Core.csproj','src/YacaPluginSwitcher/YacaPluginSwitcher.csproj','src/YacaPluginSwitcher/App.xaml','src/YacaPluginSwitcher/App.xaml.cs','src/YacaPluginSwitcher/MainWindow.xaml','src/YacaPluginSwitcher/MainWindow.xaml.cs','src/YacaPluginSwitcher/StartupWindow.xaml','src/YacaPluginSwitcher/StartupWindow.xaml.cs','src/YacaPluginSwitcher/Styles/Colors.xaml','src/YacaPluginSwitcher/Styles/Controls.xaml','src/YacaPluginSwitcher/Views/BackupView.xaml','src/YacaPluginSwitcher/Views/BackupView.xaml.cs','src/YacaPluginSwitcher/Views/ConfigView.xaml','src/YacaPluginSwitcher/Views/ConfigView.xaml.cs','src/YacaPluginSwitcher/Views/InfoView.xaml','src/YacaPluginSwitcher/Views/InfoView.xaml.cs')
foreach($r in $required){if(-not(Test-Path (Join-Path $Root $r)-PathType Leaf)){throw "Required file missing: $r"}}
foreach($p in Get-ChildItem $Root -Recurse -File -Include *.csproj,*.props,*.pubxml){[xml]$null=Get-Content $p.FullName -Raw}
foreach($f in Get-ChildItem $Root -Recurse -File -Include *.cs,*.xaml){$t=Get-Content $f.FullName -Raw;if($t-match '(?i)System\.Windows\.Forms|UseWindowsForms|WinForms|WindowsForms'){throw "Legacy WinForms reference remains: $($f.FullName)"}}
$project=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/YacaPluginSwitcher.csproj')-Raw
foreach($s in @('<TargetFramework>net10.0-windows</TargetFramework>','<UseWPF>true</UseWPF>','<SelfContained>true</SelfContained>','<PublishSingleFile>true</PublishSingleFile>','<DebugSymbols>false</DebugSymbols>','<DebugType>None</DebugType>')){if($project -notmatch [regex]::Escape($s)){throw "Project setting missing: $s"}}
$main=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/MainWindow.xaml')-Raw;$code=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/MainWindow.xaml.cs')-Raw
if($main-notmatch 'x:Name="NavPanel"'){throw 'WPF navigation host missing.'};if($main-notmatch 'x:Name="PageHost"'){throw 'WPF page host missing.'};if($main-notmatch 'WindowStyle="None"'){throw 'Custom window style missing.'};if($main-notmatch 'WindowChrome'){throw 'WPF WindowChrome missing.'}
$app=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/App.xaml')-Raw;if($app-notmatch 'Styles/Colors.xaml'-or$app-notmatch 'Styles/Controls.xaml'){throw 'WPF resource dictionaries are not merged.'}
foreach($r in @('Assets\yaca_logo.png','Assets\discord_icon.png','Assets\github_icon.png')){if(-not(Test-Path (Join-Path $Root "src/YacaPluginSwitcher/$r")-PathType Leaf)){throw "Required UI asset missing: $r"}}
foreach($v in @('BackupView','ConfigView','InfoView')){if($code -notmatch [regex]::Escape($v)){throw "Expected WPF view reference missing: $v"}}
$appPaths=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/AppPaths.cs')-Raw;if($appPaths-match 'LocalApplicationData|ApplicationData'){throw 'Portable paths must not use APPDATA or LOCALAPPDATA.'}
Write-Host 'Static preflight: PASS' -ForegroundColor Green
Write-Host 'WPF architecture checks: PASS' -ForegroundColor Green
Write-Host 'Compiler/XAML/analyzer validation is performed by dotnet build.' -ForegroundColor Yellow