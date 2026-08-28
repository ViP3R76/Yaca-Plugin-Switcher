$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

Write-Host 'YACA Plugin Switcher - static preflight' -ForegroundColor Cyan

$project = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/YacaPluginSwitcher.csproj') -Raw
foreach ($setting in @('<RuntimeIdentifier>win-x64</RuntimeIdentifier>','<SelfContained>true</SelfContained>','<PublishSingleFile>true</PublishSingleFile>','<DebugSymbols>false</DebugSymbols>','<DebugType>None</DebugType>')) {
    if ($project -notmatch [regex]::Escape($setting)) { throw "Project setting missing: $setting" }
}

foreach ($resource in @('Assets\\yaca_logo.png','Assets\\discord_icon.png','Assets\\github_icon.png')) {
    $resourcePath = Join-Path $Root "src/YacaPluginSwitcher/$resource"
    if (-not (Test-Path -LiteralPath $resourcePath -PathType Leaf)) { throw "Required UI asset missing: $resource" }
    if ($project -notmatch [regex]::Escape("<EmbeddedResource Include=\"$resource\" />")) { throw "UI asset is not embedded: $resource" }
}

$appPaths = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/AppPaths.cs') -Raw
if ($appPaths -match 'LocalApplicationData|ApplicationData') { throw 'Portable paths must not use APPDATA or LOCALAPPDATA.' }
if ($appPaths -notmatch 'SettingsFilePath\s*=\s*Path\.Combine\(BaseDirectory,\s*"config\.json"\)') { throw 'config.json must be stored beside the executable.' }
if ($appPaths -notmatch 'LogDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Logs"\)') { throw 'Logs must be stored beside the executable.' }
if ($appPaths -notmatch 'BackupDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Backups"\)') { throw 'Backups must be stored beside the executable.' }

$program = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/Program.cs') -Raw
if ($program -notmatch 'new ProfessionalMainForm\(service\)') { throw 'Professional single-window main form is not the application entry point.' }

$mainForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/ProfessionalMainForm.cs') -Raw
foreach ($required in @('BuildSidebar\(\)','BuildDashboard\(\)','Branding\.Logo','YACA UPDATER','CreateBackupFromDashboard\(\)','ShowSwitchPage\(\)','ShowBackups\(\)','ShowConfig\(\)','ShowInfo\(\)','RefreshActivePage\(true\)','LanguageChanged')) {
    if ($mainForm -notmatch $required) { throw "Professional UI feature missing: $required" }
}
$actionCalls = [regex]::Matches($mainForm, '(?m)^\s*AddAction\(').Count
if ($actionCalls -ne 3) { throw "Dashboard must contain exactly three primary action cards; found $actionCalls." }
foreach ($requiredAction in @('YACA WECHSELN','BACKUP ERSTELLEN','YACA UPDATER')) {
    if ($mainForm -notmatch [regex]::Escape($requiredAction)) { throw "Primary dashboard action missing: $requiredAction" }
}
if ($mainForm -match 'AddAction\([^\n]*Texts\.Backups') { throw 'Backups must not be duplicated in dashboard action cards.' }
if ($mainForm -match 'AddAction\([^\n]*Texts\.Config') { throw 'Configuration must not be duplicated in dashboard action cards.' }
if ($mainForm -match 'AddAction\([^\n]*Texts\.About') { throw 'Info must not be duplicated in dashboard action cards.' }
if ([regex]::Matches($mainForm, 'ColumnCount\s*=\s*3').Count -lt 2) { throw 'Dashboard status/action three-column layouts missing.' }
if ([regex]::Matches($mainForm, 'Branding\.Logo').Count -lt 2) { throw 'Design branding logo must be present in both sidebar and dashboard.' }
if ($mainForm -notmatch 'form\.MinimumSize\s*=\s*Size\.Empty' -or $mainForm -notmatch 'form\.MaximumSize\s*=\s*Size\.Empty') { throw 'Embedded pages must be allowed to fill the main window.' }
if ($mainForm -match 'MainForm_Resize') { throw 'Obsolete MainForm_Resize handler remains.' }
if ($mainForm -match 'ShowError\(ex\.Message\)|_status\.Text\s*=.*ex\.Message') { throw 'Main UI exposes raw exception messages.' }

$configForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/ConfigForm.cs') -Raw
foreach ($required in @('TeamSpeakPluginDirectories|MultipleTeamSpeakInstances','MaxBackups|AutomaticBackup|WarnIfTeamSpeakRunningOption','_language','SelectableBackups')) {
    if ($configForm -notmatch $required) { throw "Configuration feature missing: $required" }
}

$backupForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/BackupForm.cs') -Raw
foreach ($required in @('SelectableBackupsForDeletion','DeleteBackups\(\)','RestoreSelected\(\)','sha256')) {
    if ($backupForm -notmatch $required) { throw "Backup feature missing: $required" }
}
if ($backupForm -notmatch 'DataGridViewCheckBoxColumn') { throw 'Selectable backup deletion checkbox column missing.' }
if ($backupForm -notmatch 'DataGridViewContentAlignment\.MiddleCenter') { throw 'Backup deletion checkbox must be centered.' }
if ($backupForm -notmatch 'ScrollBars\s*=\s*ScrollBars\.Vertical') { throw 'Backup grid must not introduce a horizontal scrollbar.' }

$localization = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/Localization.cs') -Raw
foreach ($requiredText in @('public const string English = "en"','public const string German = "de"','GetErrorMessage\(Exception exception','ErrorInvalidYacaDll','ErrorUnexpected','AlreadyRunningMessage')) {
    if ($localization -notmatch $requiredText) { throw "Localization feature missing: $requiredText" }
}

$darkMode = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/DarkMode.cs') -Raw
if ($darkMode -notmatch 'SetWindowTheme') { throw 'Dark scrollbar P/Invoke missing.' }
if ($darkMode -notmatch 'uxtheme\.dll') { throw 'Dark scrollbar must use uxtheme.dll.' }

$teamSpeak = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/TeamSpeakDetector.cs') -Raw
if ($teamSpeak -notmatch 'EnumWindows' -or $teamSpeak -notmatch 'SendMessageTimeoutW') { throw 'Graceful TeamSpeak close implementation missing.' }
if ($teamSpeak -match '\.Kill\(') { throw 'TeamSpeak close logic must never force-kill.' }

$appError = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/AppError.cs') -Raw
if ($appError -notmatch 'enum AppErrorCode' -or $appError -notmatch 'class YacaOperationException') { throw 'Localized application error model missing.' }

$info = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/InfoPage.cs') -Raw
if ($info -notmatch 'YacaLinks' -or $info -notmatch 'TeamSpeakLinks') { throw 'Info page vendor sections missing.' }
if ($info -notmatch 'discord\.yaca\.systems') { throw 'YACA Discord link missing.' }
if ($info -notmatch 'downloads\?product=ts3') { throw 'TeamSpeak 3 client link missing.' }
if ($info -notmatch 'github\.com/ViP3R76/Yaca-Plugin-Switcher') { throw 'GitHub repository link missing.' }
if ($info -notmatch 'Branding\.DiscordIcon' -or $info -notmatch 'Branding\.GitHubIcon') { throw 'Community/GitHub branding icons missing.' }

$branding = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/Branding.cs') -Raw
foreach ($resourceName in @('YacaPluginSwitcher\.Assets\.yaca_logo\.png','YacaPluginSwitcher\.Assets\.discord_icon\.png','YacaPluginSwitcher\.Assets\.github_icon\.png')) {
    if ($branding -notmatch $resourceName) { throw "Branding resource reference missing: $resourceName" }
}

$readme = Get-Content (Join-Path $Root 'README.md') -Raw
if ($readme -notmatch '1–9|1-9') { throw 'README must document MaxBackups 1-9.' }
if ($readme -match '%APPDATA%.*configuration|%LOCALAPPDATA%.*configuration') { throw 'README contains outdated portable-path documentation.' }

$tests = Get-ChildItem -Path $Root -Recurse -File | Where-Object { $_.FullName -match '\\tests\\|Test\.csproj$|Tests\.dll$' }
if ($tests) { throw 'Test projects/artifacts must not be part of the release source.' }

Write-Host 'Static preflight: PASS' -ForegroundColor Green
Write-Host 'Run dotnet build on Windows with the .NET 10 SDK for compiler/analyzer validation.' -ForegroundColor Yellow
