param([string]$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path)
$ErrorActionPreference = 'Stop'
Write-Host 'YACA Plugin Switcher - static preflight' -ForegroundColor Cyan

$required = @(
'YacaPluginSwitcher.sln','Directory.Build.props','src/YacaPluginSwitcher.Core/YacaPluginSwitcher.Core.csproj','src/YacaPluginSwitcher/YacaPluginSwitcher.csproj',
'src/YacaPluginSwitcher/Properties/PublishProfiles/Win64SingleFile.pubxml','src/YacaPluginSwitcher/Program.cs','src/YacaPluginSwitcher/ProfessionalMainForm.cs',
'src/YacaPluginSwitcher/UI/ProfessionalVisuals.cs','src/YacaPluginSwitcher/UI/Branding.cs','src/YacaPluginSwitcher/UI/Theme.cs',
'src/YacaPluginSwitcher/UI/ConfigForm.cs','src/YacaPluginSwitcher/UI/BackupForm.cs','src/YacaPluginSwitcher/UI/InfoPage.cs',
'src/YacaPluginSwitcher.Core/AppError.cs','src/YacaPluginSwitcher.Core/Localization.cs','src/YacaPluginSwitcher.Core/AppPaths.cs')
foreach($relative in $required){if(-not(Test-Path(Join-Path $Root $relative)-PathType Leaf)){throw "Required file missing: $relative"}}

$projects=Get-ChildItem -Path $Root -Recurse -File -Include *.csproj,*.props,*.pubxml
foreach($project in $projects){[xml]$null=Get-Content -LiteralPath $project.FullName -Raw}
$csFiles=Get-ChildItem -Path $Root -Recurse -File -Filter *.cs
foreach($file in $csFiles){$text=Get-Content -LiteralPath $file.FullName -Raw;if($text-match'(?i)<PackageReference|Newtonsoft|MahApps|MaterialDesign'){throw "External dependency marker found: $($file.FullName)"};if(([regex]::Matches($text,'\{')).Count-ne([regex]::Matches($text,'\}')).Count){throw "Brace count mismatch: $($file.FullName)"}}

$project=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/YacaPluginSwitcher.csproj')-Raw
foreach($setting in @('<RuntimeIdentifier>win-x64</RuntimeIdentifier>','<SelfContained>true</SelfContained>','<PublishSingleFile>true</PublishSingleFile>','<DebugSymbols>false</DebugSymbols>','<DebugType>None</DebugType>')){if($project-notmatch[regex]::Escape($setting)){throw "Project setting missing: $setting"}}
foreach($resource in @('Assets\yaca_logo.png','Assets\discord_icon.png','Assets\github_icon.png')){$path=Join-Path $Root "src/YacaPluginSwitcher/$resource";if(-not(Test-Path -LiteralPath $path -PathType Leaf)){throw "Required UI asset missing: $resource"};$embedded='<EmbeddedResource Include="{0}" />'-f $resource;if($project-notmatch[regex]::Escape($embedded)){throw "UI asset is not embedded: $resource"}}

$appPaths=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/AppPaths.cs')-Raw
if($appPaths-match'LocalApplicationData|ApplicationData'){throw 'Portable paths must not use APPDATA or LOCALAPPDATA.'}
foreach($pathCheck in @('SettingsFilePath\s*=\s*Path\.Combine\(BaseDirectory,\s*"config\.json"\)','LogDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Logs"\)','BackupDirectory\s*=\s*Path\.Combine\(BaseDirectory,\s*"Backups"\)')){if($appPaths-notmatch$pathCheck){throw "Portable path check failed: $pathCheck"}}

$program=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/Program.cs')-Raw
if($program-notmatch'new ProfessionalMainForm\(service\)'){throw 'Professional single-window main form is not the application entry point.'}
if($program-notmatch'ProfessionalVisuals\.Apply\(form\)'){throw 'Professional visual system is not applied at startup.'}

$mainForm=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/ProfessionalMainForm.cs')-Raw
foreach($token in @('class ProfessionalMainForm','BuildSidebar\(\)','BuildDashboard\(\)','Branding\.Logo','YACA WECHSELN','BACKUP ERSTELLEN','YACA UPDATER','ShowSwitchPage\(\)','ShowBackups\(\)','ShowConfig\(\)','ShowInfo\(\)','RefreshActivePage\(true\)','LanguageChanged','CloseTeamSpeak\(\)','CreateBackupFromDashboard\(\)')){if($mainForm-notmatch$token){throw "Professional UI/reference missing: $token"}}
$actionCalls=[regex]::Matches($mainForm,'(?m)^\s*AddAction\(').Count
if($actionCalls-ne 3){throw "Dashboard must contain exactly three visual action tiles; found $actionCalls."}
foreach($action in @('YACA WECHSELN','BACKUP ERSTELLEN','YACA UPDATER')){if($mainForm-notmatch[regex]::Escape($action)){throw "Primary dashboard action missing: $action"}}
if($mainForm-notmatch'MinimumSize\s*=\s*new Size\(1100, 720\)'){throw 'Professional main form minimum size is missing.'}
if($mainForm-notmatch'form\.MinimumSize\s*=\s*Size\.Empty' -or $mainForm-notmatch'form\.MaximumSize\s*=\s*Size\.Empty'){throw 'Embedded pages must be allowed to fill the main window.'}
if($mainForm-match'MainForm_Resize'){throw 'Obsolete MainForm_Resize handler remains.'}

$visuals=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/ProfessionalVisuals.cs')-Raw
foreach($token in @('ProfessionalVisuals','Apply\(Control root\)','IconPainter','DrawAction','NavIcon','ActionIcon','DrawWaves','YACA')){if($visuals-notmatch$token){throw "Professional visual asset/renderer missing: $token"}}

$typeChecks=@{'YacaPluginInfo'='src/YacaPluginSwitcher.Core;src/YacaPluginSwitcher';'YacaService'='src/YacaPluginSwitcher.Core;src/YacaPluginSwitcher';'UiText'='src/YacaPluginSwitcher.Core';'YacaOperationException'='src/YacaPluginSwitcher.Core';'Localization'='src/YacaPluginSwitcher.Core';'TeamSpeakDetector'='src/YacaPluginSwitcher.Core';'BackupForm'='src/YacaPluginSwitcher/UI';'ConfigForm'='src/YacaPluginSwitcher/UI';'InfoPage'='src/YacaPluginSwitcher/UI';'Branding'='src/YacaPluginSwitcher/UI';'Theme'='src/YacaPluginSwitcher/UI'}
foreach($type in $typeChecks.Keys){$found=$false;foreach($dirRel in($typeChecks[$type]-split';')){$dir=Join-Path $Root $dirRel;if(Test-Path $dir){if(Get-ChildItem $dir -Recurse -File -Filter '*.cs'|Select-String -Pattern "\b(class|sealed class|static class|record|enum|interface)\s+$type\b" -Quiet){$found=$true;break}}};if(-not$found){throw "Referenced custom type is not defined in the solution: $type"}}

$config=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/ConfigForm.cs')-Raw
foreach($token in @('TeamSpeakPluginDirectories|MultipleTeamSpeakInstances','MaxBackups|AutomaticBackup|WarnIfTeamSpeakRunningOption','_language','SelectableBackups')){if($config-notmatch$token){throw "Configuration feature missing: $token"}}
$backup=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/BackupForm.cs')-Raw
foreach($token in @('SelectableBackupsForDeletion','DeleteBackups\(\)','RestoreSelected\(\)','sha256','DataGridViewCheckBoxColumn','DataGridViewContentAlignment\.MiddleCenter','ScrollBars\s*=\s*ScrollBars\.Vertical')){if($backup-notmatch$token){throw "Backup feature missing: $token"}}

$localization=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/Localization.cs')-Raw
foreach($token in @('public const string English = "en"','public const string German = "de"','GetErrorMessage\(Exception exception','ErrorInvalidYacaDll','ErrorUnexpected','AlreadyRunningMessage')){if($localization-notmatch$token){throw "Localization feature missing: $token"}}
$darkMode=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/DarkMode.cs')-Raw
if($darkMode-notmatch'SetWindowTheme' -or $darkMode-notmatch'uxtheme\.dll'){throw 'Dark scrollbar implementation missing.'}
$teamSpeak=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/TeamSpeakDetector.cs')-Raw
if($teamSpeak-notmatch'EnumWindows' -or $teamSpeak-notmatch'SendMessageTimeoutW'){throw 'Graceful TeamSpeak close implementation missing.'}
if($teamSpeak-match'\.Kill\('){throw 'TeamSpeak close logic must never force-kill.'}
$appError=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher.Core/AppError.cs')-Raw
if($appError-notmatch'enum AppErrorCode' -or $appError-notmatch'class YacaOperationException'){throw 'Localized application error model missing.'}

$info=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/InfoPage.cs')-Raw
foreach($token in @('YacaLinks','TeamSpeakLinks','discord\.yaca\.systems','downloads\?product=ts3','github\.com/ViP3R76/Yaca-Plugin-Switcher','Branding\.DiscordIcon','Branding\.GitHubIcon')){if($info-notmatch$token){throw "Info/link feature missing: $token"}}
$branding=Get-Content(Join-Path $Root 'src/YacaPluginSwitcher/UI/Branding.cs')-Raw
foreach($token in @('YacaPluginSwitcher\.Assets\.yaca_logo\.png','YacaPluginSwitcher\.Assets\.discord_icon\.png','YacaPluginSwitcher\.Assets\.github_icon\.png','public static Bitmap Logo')){if($branding-notmatch$token){throw "Branding resource/reference missing: $token"}}
$tests=Get-ChildItem -Path $Root -Recurse -File|Where-Object{$_.FullName-match'\\tests\\|Test\.csproj$|Tests\.dll$'}
if($tests){throw 'Test projects/artifacts must not be part of the release source.'}

Write-Host 'Static preflight: PASS' -ForegroundColor Green
Write-Host 'Type/reference sanity: PASS' -ForegroundColor Green
Write-Host 'UI asset/resource validation: PASS' -ForegroundColor Green
Write-Host 'Run dotnet build on Windows with the .NET 10 SDK for compiler/analyzer validation.' -ForegroundColor Yellow
