# YACA Plugin Switcher

A lightweight Windows utility for managing and switching between YACA TeamSpeak 3 plugin builds.

> **Status:** 1.0.0 development baseline — .NET 10 / Windows x64.

## Architecture

The project is split into a framework-neutral `YacaPluginSwitcher.Core` library and the Windows Forms application. The end-user application is published as a single self-contained Windows x64 executable via the `Win64SingleFile` publish profile.

The core application does not contain an updater. A future `YacaPluginUpdater` will be a separate executable.

## Features

- Automatic initial language detection from the Windows UI culture (German -> German; all other languages -> English)
- Manual language selection remains available and is persisted

- Automatic discovery of YACA DLLs in `Plugins`
- No hard-coded YACA version list
- Version **and Build** detection from the DLL itself
- User-facing format: `YACA 1.8.3 (Build: 1787608028)`
- x64 PE validation without loading the YACA DLL
- Multiple YACA-specific signature checks
- SHA-256 integrity information
- TeamSpeak 3 process detection
- Automatic backup before switching
- Backup restoration with SHA-256 validation
- Automatic rollback when a post-install verification fails and a backup exists
- Backup deletion with confirmation; optional per-backup selection via Expert Settings
- Configurable TeamSpeak plugin directory via application settings
- Local `Plugins` folder button opens the plugin library next to the executable
- English/German interface (English default)
- Dark-mode UI throughout the application
- Single-instance protection
- General and expert diagnostic logging
- About dialog with YACA, TeamSpeak 3 and community/legal links
- No third-party NuGet dependencies
- .NET 10
- Self-contained Windows x64 single-file publishing

## Requirements

### End users

- Windows 10/11 x64
- TeamSpeak 3 Client
- A valid YACA Windows x64 plugin

The release executable is self-contained. End users do **not** need to install the .NET runtime separately.
The application itself is designed as a single core executable; YACA versions are supplied dynamically through the local `Plugins` folder.

### Development

The solution targets **.NET 10 for Windows**. A normal `dotnet build -c Release` builds the production application only. The self-contained single-file settings are isolated in the `Win64SingleFile` publish profile.


- JetBrains Rider or another .NET 10 compatible IDE
- .NET 10 SDK
- Windows x64 development environment

## Installation

1. Download the latest YACA Plugin Switcher release.
2. Extract the application.
3. Put the YACA Windows x64 DLLs you are licensed to use into the `Plugins` directory.
4. Start `YacaPluginSwitcher.exe`.
5. Use **Plugins Folder** to open the local `Plugins` directory containing the YACA DLLs.

The switcher is **not** a YACA installer and does not grant or transfer YACA rights. The **Plugins Folder** button opens the local `Plugins` folder next to the executable; it does not change the TeamSpeak installation path.

## Dynamic YACA Detection

The switcher does not use the DLL filename or a hard-coded version list as the source of truth.

Each DLL is inspected without loading it into the application process:

1. File exists and is readable.
2. Windows PE header is valid.
3. Machine type is AMD64/x64.
4. The file is marked as a DLL.
5. Multiple YACA-specific binary signatures are present.
6. An embedded `major.minor.patch.build` version is found.
7. SHA-256 is calculated.

A valid new YACA build can therefore be added by placing the DLL into `Plugins`. The switcher automatically adds it to the menu on the next scan, provided the YACA binary format remains recognizable.

Example:

```text
Plugins/
├── yaca_175_win64.dll
├── yaca_181_win64.dll
├── yaca_182_win64.dll
├── yaca_183_win64.dll
└── yaca_184_win64.dll
```

The filename is not used as the version identity.

## Version and Build

The build number is deliberately shown next to the public version:

```text
YACA 1.8.3 (Build: 1787608028)
```

This allows users to distinguish multiple builds that share the same public version number.

## Switching Process

Before a switch, the application:

1. Detects the currently installed YACA DLL.
2. Checks whether TeamSpeak 3 is running.
3. Revalidates the selected source DLL.
4. Creates a backup when automatic backups are enabled.
5. Copies the source to a unique temporary file.
6. Validates the temporary file and its SHA-256 hash.
7. Replaces `yaca_win64.dll`.
8. Validates the target file again.
9. Automatically attempts a rollback from the created backup if post-install verification fails.

The YACA DLL is never loaded into the switcher process.

## Backups

Backups are stored **exclusively beside the executable** so the application folder remains self-contained and portable:

```text
YacaPluginSwitcher\
├── YacaPluginSwitcher.exe
├── Plugins\
└── Backups\
    └── YYYY-MM-DD_HHMMSS_mmm_GUID\
        ├── yaca_win64.dll
        └── backup.json
```

The default retention limit is 4 backups. The configured limit is restricted to 1–9. Older backups are removed after a successful switch when the configured limit is exceeded.

Backup restoration validates both the YACA format and the stored SHA-256 before the file is restored.

## Configuration

The application is portable. No configuration, log, or backup data is written to `%APPDATA%` or `%LOCALAPPDATA%`.

Configuration is stored beside the executable:

```text
YacaPluginSwitcher.exe
config.json
Logs\YacaPluginSwitcher-YYYY-MM-DD.log
Plugins\
Backups\
```

Use the **Configuration** button to change all supported settings:

- Language (`English` / `Deutsch`)
- Active TeamSpeak 3 plugin directory
- Multiple TeamSpeak 3 plugin directories
- Automatic detection of the standard TeamSpeak 3 plugin path
- Maximum number of backups (1–9, default 4)
- Automatic backup before switching
- Warning when TeamSpeak 3 is running
- Expert settings for advanced logging and multiple TeamSpeak 3 installations

The automatically detected TeamSpeak 3 plugin directory is used as the default. Custom installations can be configured under **Expert settings**.

The active path is validated before saving; if `yaca_win64.dll` is missing, the application warns that the wrong folder may have been selected or that YACA may not be installed yet.

Settings are created/updated atomically through a temporary file.

## Error handling and localization

All user-facing application errors are presented through the centralized German/English localization layer. Raw .NET exception text is not normally shown in the UI; technical exception details are retained for diagnostics/logging where appropriate.

The application automatically selects German on German Windows installations and falls back to English for all other Windows UI languages. The language can be changed manually in **Configuration**.

## Logs

Logs are stored beside the executable in the `Logs` directory. A separate log file is used for each calendar day:

```text
Logs\YacaPluginSwitcher-YYYY-MM-DD.log
```

Log retention is fixed at **3 days** and cannot be configured. At application startup, log files older than three days are automatically deleted. Cleanup is restricted to the application-local `Logs` directory; no log data is stored in `%APPDATA%` or `%LOCALAPPDATA%`.

Do not publish logs containing personal paths or other private information in public GitHub issues.

## Build in Rider

Open the solution:

```text
YacaPluginSwitcher.sln
```

Select `Release | x64` and build.

### Release build

The intended developer workflow is simply:

```text
dotnet build -c Release
```

A Release build performs the self-contained `win-x64` publish automatically and creates the ready-to-run package below the repository's `releases` directory:

```text
releases\
└── YacaPluginSwitcher-v1.0.0-win-x64.zip
```

The ZIP contains the self-contained executable and the local `Plugins` directory. No separate runtime installation is required for end users.

## Repository Structure

```text
YacaPluginSwitcher/
├── src/
│   ├── YacaPluginSwitcher.Core/
│   └── YacaPluginSwitcher/
│       ├── UI/
│       ├── Program.cs
│       └── MainForm.cs
├── Plugins/
├── docs/
├── scripts/
├── .github/
│   ├── ISSUE_TEMPLATE/
│   ├── PULL_REQUEST_TEMPLATE.md
│   └── workflows/
├── Directory.Build.props
├── YacaPluginSwitcher.sln
├── LICENSE
├── CHANGELOG.md
├── CONTRIBUTING.md
├── SECURITY.md
└── README.md
```

## Validation

The repository is validated with static preflight checks and the production Release build. There is no separate test project in the release repository.

Run `scripts/preflight.ps1` for the repository's static preflight checks. The actual compiler/analyzer build should be run on Windows with the .NET 10 SDK installed. The repository is configured with warnings as errors so compiler and analyzer regressions fail the build instead of being silently ignored.

## Third-Party Software

### YACA

YACA is a third-party TeamSpeak 3 plugin. Obtain YACA separately from its official provider and comply with the applicable license terms.

- https://yaca.systems/
- https://yaca.systems/licenses
- https://yaca.systems/tos
- https://yaca.systems/faq

YACA binaries are intentionally not included in the source repository by default.

### TeamSpeak 3

TeamSpeak 3 is third-party software. This project does not distribute TeamSpeak software.

- https://www.teamspeak.com/
- https://support.teamspeak.com/
- https://support.teamspeak.com/hc/en-us/articles/360002712358-How-do-I-write-my-own-plugins
- https://support.teamspeak.com/hc/en-us/sections/360000716518-Licenses

## Legal / Trademark Notice

YACA Plugin Switcher is an independent third-party application created by **ViP3R_76**.

This project is not affiliated with, endorsed by, sponsored by, or officially associated with YACA or TeamSpeak Systems GmbH.

YACA and TeamSpeak / TeamSpeak 3 are trademarks or property of their respective owners. All rights remain with their respective owners.

The MIT License in this repository applies only to the YACA Plugin Switcher source code. It does not grant rights to YACA or TeamSpeak software, binaries, trademarks, documentation or other third-party materials.

## Community

**Author:** ViP3R_76

**Discord:** https://discord.gg/9AxuZkyU7P

## License

The YACA Plugin Switcher source code is released under the MIT License. See `LICENSE`.


## Building a Release

The application targets **.NET 10 for Windows x64**. A normal Rider build is used for development. To create the distributable, run the `Win64SingleFile` publish profile or execute:

```powershell
.\scripts\publish-release.ps1
```

After a successful publish, the project automatically creates a ready-to-run ZIP in `./releases`. The package contains the published self-contained executable and the local `Plugins` directory. The ZIP contains the self-contained `YacaPluginSwitcher.exe` and a `Plugins` directory.

The release build is self-contained, so end users do not need to install the .NET Desktop Runtime separately. No third-party runtime or NuGet package installation is required. No third-party runtime, NuGet package, or external library installation is required.

## TeamSpeak 3 target modes

The configuration contains an explicit **multiple TeamSpeak 3 installations** option. It is disabled by default.

- **Disabled:** the switcher uses the automatically detected/default TeamSpeak 3 plugin directory. The single target path can still be adjusted manually in Config.
- **Enabled:** the configured list of TeamSpeak 3 plugin directories becomes active and one of those paths can be selected as the current target.

The multiple-instance list is ignored while the option is disabled, which also keeps older configurations in the single-instance behavior.


## Backup deletion
Backup deletion is protected by confirmation. By default the delete action removes all backups. Expert Settings can enable **Selectable Backups for deletion**, allowing individual backup entries to be marked with checkboxes before deletion.

## Backup management

The **Delete Backups** action always asks for confirmation. By default it removes all backups. Under **Expert Settings**, enable **Selectable Backups for deletion** to show a checkbox for each backup and delete only the marked entries. Deletion is restricted to backup directories inside the portable `Backups` directory.

## Release packaging

Release output is always written relative to the repository/solution root into `releases/`, independent of Rider's working directory. Production releases are self-contained .NET 10 win-x64 single-file executables and intentionally contain no PDB, DLL, `.deps.json`, or `.runtimeconfig.json` files. The release script also creates a SHA-256 checksum and validates the final ZIP contents.

The fixed log retention is three days. Log cleanup runs at application startup and only operates inside the application's local `Logs` directory.
