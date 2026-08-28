## 1.0.0 – Stable .NET 10 baseline

- Stable validated YACA Plugin Switcher baseline.
- Automated Windows x64 single-file release packaging is provided by GitHub Actions.
- Tagged releases receive a user-ready ZIP and SHA-256 checksum.

## Unreleased – Log retention

- Logs are written to one file per calendar day.
- Log retention is fixed to 3 days and is not user-configurable.
- At startup, log files older than three days are automatically removed.
- Cleanup is restricted to the application-local `Logs` directory.

## Unreleased – Backup UI fixes

- Individual backup deletion can be enabled through Expert Settings.
- Backup entries use visible native selection checkboxes without owner-draw flicker.
- Backup columns are content-sized and the window can resize horizontally and vertically.

## Current UI / Localization hardening

- English/German UI with automatic Windows language detection and English fallback.
- Consistent dark-mode styling.
- Portable config, logs and backups beside the executable.
- Single-instance protection.
- Localized user-facing operation errors.
- YACA DLL validation without loading YACA binaries into the switcher process.
