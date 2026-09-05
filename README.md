# YACA Plugin Switcher

[![Latest Release](https://img.shields.io/github/v/release/ViP3R76/Yaca-Plugin-Switcher?display_name=tag&style=for-the-badge&logo=github)](https://github.com/ViP3R76/Yaca-Plugin-Switcher/releases/latest)
[![GitHub Downloads](https://img.shields.io/github/downloads/ViP3R76/Yaca-Plugin-Switcher/latest/total?style=for-the-badge&logo=github)](https://github.com/ViP3R76/Yaca-Plugin-Switcher/releases/latest)

A lightweight Windows application for managing, validating, backing up and switching between YACA TeamSpeak 3 plugin builds.

## Current Release

**Version 1.1.0** — Windows x64, .NET 10, self-contained.

urlDownload the latest releasehttps://github.com/ViP3R76/Yaca-Plugin-Switcher/releases/latest

## Features

- Automatic discovery of YACA DLLs in the local `Plugins` folder
- Version **and Build** detection directly from the DLL
- x64 PE validation and YACA-specific signature checks without loading the DLL
- SHA-256 integrity information
- Safe YACA plugin switching with temporary-file validation
- Automatic backup before switching
- Backup restoration with integrity validation
- Automatic rollback when post-install verification fails
- Configurable backup retention
- Optional selective backup deletion in Expert Settings
- Automatic TeamSpeak 3 process detection and configurable plugin directories
- Built-in updater for available YACA plugin versions
- Download, extraction, verification, validation and installation progress
- English / German interface with automatic initial language detection
- Dark-mode WPF user interface
- Portable configuration, backups and logs beside the application
- Single-instance protection and diagnostic logging

## Requirements

### End users

- Windows 10/11 x64
- TeamSpeak 3 Client
- A valid YACA Windows x64 plugin

The release is self-contained. No separate .NET runtime installation is required.

### Development

- Windows x64
- .NET 10 SDK
- JetBrains Rider or another .NET 10 compatible IDE

## Installation

1. Download the latest release ZIP.
2. Extract it to a folder of your choice.
3. Start `YacaPluginSwitcher.exe`.
4. Place the YACA Windows x64 DLLs you are licensed to use into the local `Plugins` folder.
5. Use **Plugins Folder** in the application to open that folder when needed.

YACA binaries and TeamSpeak software are **not** distributed with this project.

## YACA Detection

YACA versions are not identified from filenames or from a hard-coded version list. The application inspects each DLL and checks its Windows PE/x64 characteristics, YACA-specific signatures and embedded version information before accepting it.

A recognized new YACA build can therefore be added by placing its DLL into `Plugins`.

Example:

```text
YacaPluginSwitcher/
├── YacaPluginSwitcher.exe
├── Plugins/
│   ├── yaca_181_win64.dll
│   ├── yaca_182_win64.dll
│   └── ...
├── Backups/
├── Logs/
└── config.json
```

## Switching & Backups

Before a switch, the selected source DLL is revalidated. When automatic backups are enabled, the currently installed plugin is backed up before replacement. The new file is copied through a temporary path, validated and installed as `yaca_win64.dll`.

If post-install verification fails, the application can automatically restore the created backup.

Backups are kept locally beside the executable. The default retention limit is **4 backups** and can be configured from 1–9.

## Updater

The integrated updater can discover available YACA plugin versions and download selected versions into the local application storage. The installation flow verifies the downloaded archive before the plugin DLL is moved into `Plugins`.

The updater reports the actual processing chain:

1. Download
2. Extraction
3. Verification
4. Validation
5. Move
6. Download cleanup / keep
7. Completed

## Configuration & Logs

The application is portable. Configuration, backups and logs are stored beside the executable rather than in `%APPDATA%` or `%LOCALAPPDATA%`.

Available settings include language, TeamSpeak 3 plugin directories, automatic backups, backup retention, TeamSpeak warnings, updater behavior and Expert Settings.

Logs are written to `Logs\YacaPluginSwitcher-YYYY-MM-DD.log`. Log retention is limited to three days and cleanup is restricted to the application-local log directory.

## Build

Open `YacaPluginSwitcher.sln` in Rider and build the `Release | x64` configuration.

For a normal development build:

```powershell
dotnet build -c Release
```

Release packaging is handled by the repository's GitHub Actions workflow. The production package is a self-contained Windows x64 single-file application and is accompanied by a SHA-256 checksum.

## Project Structure

```text
YacaPluginSwitcher/
├── src/
│   ├── YacaPluginSwitcher.Core/
│   └── YacaPluginSwitcher/
│       ├── Assets/
│       ├── Styles/
│       ├── Views/
│       └── MainWindow.*
├── scripts/
├── Build/
├── .github/workflows/
├── YacaPluginSwitcher.sln
├── LICENSE
├── CHANGELOG.md
└── README.md
```

The application is implemented as a WPF frontend with a framework-neutral Core library. Centralized WPF resource dictionaries provide the application-wide colors, controls, navigation, DataGrid, scrollbar and ScrollViewer styling.

## Third-Party Software

YACA and TeamSpeak 3 are third-party products. This project is independent and does not distribute their software or grant any rights to their binaries or trademarks.

- YACA: https://yaca.systems/
- TeamSpeak: https://www.teamspeak.com/

## Legal Notice

YACA Plugin Switcher is an independent third-party application created by **ViP3R_76**. It is not affiliated with, endorsed by, sponsored by, or officially associated with YACA or TeamSpeak Systems GmbH.

YACA and TeamSpeak / TeamSpeak 3 are trademarks or property of their respective owners. The MIT License in this repository applies only to the YACA Plugin Switcher source code.

## Community

**Author:** ViP3R_76  
**Discord:** https://discord.gg/9AxuZkyU7P

## License

The YACA Plugin Switcher source code is released under the MIT License. See `LICENSE`.
