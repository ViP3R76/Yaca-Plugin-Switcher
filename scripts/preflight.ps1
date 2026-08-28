$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot

Write-Host 'YACA Plugin Switcher - static preflight' -ForegroundColor Cyan

$publishProfile = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/YacaPluginSwitcher.csproj') -Raw
foreach ($setting in @('<RuntimeIdentifier>win-x64</RuntimeIdentifier>','<SelfContained>true</SelfContained>','<PublishSingleFile>true</PublishSingleFile>')) {
    if ($publishProfile -notmatch [regex]::Escape($setting)) { throw "Publish profile missing: $setting" }
}

$appPaths = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher.Core/AppPaths.cs') -Raw
if ($appPaths -match 'LocalApplicationData|ApplicationData') { throw 'Portable paths must not use APPDATA or LOCALAPPDATA.' }
if ($appPaths -notmatch 'SettingsFilePath\s*=\s*Path\.Combine\(BaseDirectory,\s*"config\.json"\)') { throw 'config.json must be stored beside the executable.' }
if ($appPaths -notmatch 'LogDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Logs"\)') { throw 'Logs must be stored beside the executable.' }
if ($appPaths -notmatch 'BackupDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Backups"\)') { throw 'Backups must be stored beside the executable.' }

$mainForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/MainForm.cs') -Raw
if ($mainForm -notmatch 'OpenPluginsFolder\(\)') { throw 'Local Plugins folder action missing.' }
if ($mainForm -match 'SetTargetDirectory\(dialog\.SelectedPath\)') { throw 'Plugins Folder must not change target directory.' }
if ($mainForm -notmatch 'FormBorderStyle\s*=\s*FormBorderStyle\.Sizable') { throw 'Main form must be resizable.' }
if ($mainForm -notmatch 'MinimumSize\s*=\s*new Size\(900, 640\)') { throw 'Main form minimum size is incorrect.' }
if ($mainForm -notmatch 'ShowEmbeddedPage\(') { throw 'Single-window embedded page navigation is missing.' }
if ($mainForm -notmatch 'new BackupForm\(_service\)') { throw 'Backup page integration is missing.' }
if ($mainForm -notmatch 'new ConfigForm\(_service\)') { throw 'Configuration page integration is missing.' }
if ($mainForm -notmatch 'new AboutForm\(_service\.Settings\.Language\)') { throw 'Info page integration is missing.' }
if ($mainForm -notmatch 'CreateBackupFromDashboard\(\)') { throw 'Dashboard backup action is missing.' }
if ($mainForm -notmatch 'CloseTeamSpeak\(\)') { throw 'TeamSpeak close action is missing.' }
if ($mainForm -match 'MainForm_Resize') { throw 'Obsolete MainForm_Resize handler remains.' }
if ($mainForm -match 'ShowError\(ex\.Message\)|_status\.Text\s*=.*ex\.Message') { throw 'MainForm exposes raw exception messages.' }

$configForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/ConfigForm.cs') -Raw
if ($configForm -notmatch 'TeamSpeakPluginDirectories|MultipleTeamSpeakInstances') { throw 'Multi-instance configuration missing.' }
if ($configForm -notmatch 'MaxBackups|AutomaticBackup|WarnIfTeamSpeakRunningOption') { throw 'Application options missing.' }
if ($configForm -notmatch '_language') { throw 'Language selector missing.' }
if ($configForm -notmatch 'SelectableBackups') { throw 'Selectable backup deletion option missing.' }

$backupForm = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/BackupForm.cs') -Raw
if ($backupForm -notmatch 'SelectableBackupsForDeletion') { throw 'Selectable backup deletion behavior missing.' }
if ($backupForm -notmatch 'DeleteBackups\(\)') { throw 'Backup deletion action missing.' }
if ($backupForm -notmatch 'RestoreSelected\(\)') { throw 'Backup restore action missing.' }
if ($backupForm -notmatch 'sha256') { throw 'Backup SHA-256 display/verification missing.' }

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

$about = Get-Content (Join-Path $Root 'src/YacaPluginSwitcher/UI/AboutForm.cs') -Raw
if ($about -notmatch 'SnakeNest Community - by ViP3R_76') { throw 'SnakeNest Community label missing.' }
if ($about -notmatch 'discord\.yaca\.systems') { throw 'YACA Discord link missing.' }
if ($about -notmatch 'downloads\?product=ts3') { throw 'TeamSpeak 3 client link missing.' }

$readme = Get-Content (Join-Path $Root 'README.md') -Raw
if ($readme -notmatch '1–9|1-9') { throw 'README must document MaxBackups 1-9.' }
if ($readme -match '%APPDATA%.*configuration|%LOCALAPPDATA%.*configuration') { throw 'README contains outdated portable-path documentation.' }

Write-Host 'Static preflight: PASS' -ForegroundColor Green
Write-Host 'Run dotnet build/test on Windows with the .NET 10 SDK for compiler/analyzer validation.' -ForegroundColor Yellow

$Project = Join-Path $Root 'src\YacaPluginSwitcher\YacaPluginSwitcher.csproj'
$ProjectText = Get-Content $Project -Raw
if ($ProjectText -notmatch '<DebugSymbols>false</DebugSymbols>') { throw 'Release must disable debug symbols.' }
if ($ProjectText -notmatch '<DebugType>None</DebugType>') { throw 'Release must disable PDB generation.' }
Write-Host 'PDB generation disabled: PASS' -ForegroundColor Green
