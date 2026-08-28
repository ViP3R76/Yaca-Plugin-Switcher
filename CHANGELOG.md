## Unreleased – Log retention

- Logs are now written to one file per calendar day.
- Log retention is fixed to 3 days and is not user-configurable.
- At startup, log files older than three days are automatically removed.
- Cleanup is restricted to the application-local `Logs` directory.
- Legacy `YacaPluginSwitcher.log` files are also cleaned up when older than the retention period.


## Unreleased – Backup UI fixes

- Persisted the individual-backup deletion option even when Expert Settings are hidden.
- Restored visible native backup-selection checkboxes.
- Removed content truncation caused by forced column compression.
- Backup window can resize horizontally and vertically within the screen bounds.
- Backup columns are sized to their actual content and the form minimum width adapts accordingly.
- Normalized spacing for General Logging, Debug Logging, and selectable backup deletion in Expert Settings.

# Unreleased

- Backup screen: selectable deletion checkboxes are rendered by the native ListView.
- Backup columns now size to their content and adapt to the available horizontal space.
- Backup window can be resized horizontally and vertically.
- Removed custom owner-draw code that caused mouse-over flicker.
- Added localized delete-column header (German/English).


## Current UI / TeamSpeak Fixes

- Main window is fully resizable with a safe minimum size.
- Plugin buttons resize with the window.
- TeamSpeak close button widened for localized text.
- TeamSpeak shutdown now requests WM_CLOSE on detected top-level windows as a graceful fallback; no force-kill is used.
# Changelog

## Unreleased / Build fixes

- Fixed nullable `Font` handling in themed ComboBox and backup ListView rendering.
- Fixed stale `_invalid` UI reference in `MainForm`.
- Fixed `AboutForm` link handler instance access.
- Added explicit null guards for backup ListView draw events.
- Kept .NET 10 / x64 / self-contained release architecture unchanged.

## Unreleased

- Added English/German UI language selection with English as the default.
- Added consistent dark-mode styling to all application dialogs.
- Changed the Plugins Folder button to open the local plugin library next to the executable.
- Moved backups exclusively beside the executable.
- Added single-instance protection.
- Refined main-window layout and centered action buttons.
- Removed the redundant prefix from the active-version display.


## 1.0.0 – .NET 10 baseline

- Migrated the application target from .NET 8 to .NET 10.
- Added strict nullable reference type handling.
- Added warnings-as-errors build policy.
- Fixed `BackupInfo.DisplayName` reference consistency.
- Updated GitHub Actions to .NET 10.
- Improved backup path handling and metadata validation.
- Added automatic rollback after failed post-install verification.
- Improved temporary-file cleanup and exception filtering.
- Configuration, logs, and backups are stored beside the executable so the application remains portable.

## Current UI / Localization hardening

- Localized user-facing operation errors through a centralized `AppErrorCode` / `YacaOperationException` mapping.

- Fixed missing `SetWindowTheme` P/Invoke declaration used by the dark scrollbar handling.
- Centralized user-facing operation errors through localized error mapping for German and English.
- Removed raw exception messages from normal user dialogs; technical exception details remain available to diagnostics/logging.
- Localized startup status messages and single-instance notification.
- Added a static repository preflight check for the localization/error surface.
- Corrected portable configuration/logging documentation and the backup retention documentation.

### Backup UI hardening
- Reworked backup list to a native double-buffered DataGridView.
- Added centered selectable deletion checkboxes without owner-draw flicker.
- SHA-256 values are displayed in full.
- Dark header/cell styling is applied consistently.
- Columns are content-sized and no unused extra column is rendered.
- Backup window remains horizontally and vertically resizable without stretching columns.
