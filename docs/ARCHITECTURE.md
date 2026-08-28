# Architecture

## Projects

- `YacaPluginSwitcher.Core` — framework-neutral application logic and models.
- `YacaPluginSwitcher` — Windows Forms executable and presentation layer.

The test project must never reference the executable project. This avoids `NETSDK1151` when the application is published self-contained.

## Core modules

- `Configuration` — paths and persistent settings.
- `Models` — immutable data structures exchanged between modules.
- `Core` — scanner, validator, installer, backup, hashing, logging and TeamSpeak detection.

## Runtime / publishing

The application targets `.NET 10 for Windows` and `win-x64` for distribution. Self-contained/single-file settings live only in `Properties/PublishProfiles/Win64SingleFile.pubxml`.

The distributed application remains self-contained; no test project or third-party runtime dependency is shipped.

## DLL handling

YACA DLLs are treated as untrusted input. The switcher reads PE/binary data only and does not load the DLL into its own process.

Validation pipeline:

```text
File
 -> PE/x64 validation
 -> YACA signature validation
 -> Version/build extraction
 -> SHA-256
 -> YacaPluginInfo
```

## Installation safety

```text
source validation
 -> optional backup
 -> temporary copy
 -> temporary validation
 -> target replacement
 -> target validation
 -> rollback on failure
 -> retention cleanup after success
```

A failed update never intentionally leaves a partially copied temporary DLL as the active YACA plugin.

## Future updater

The switcher deliberately contains no network updater. A future `YacaPluginUpdater` will be a separate executable and may reuse the Core project where appropriate.

### TeamSpeak target selection

`AppSettings.UseMultipleTeamSpeakInstances` is the explicit feature switch and defaults to `false`.

- `false`: `YacaService` uses the detected TeamSpeak 3 plugin directory. `UseCustomTeamSpeakPluginDirectory` is only set when the user deliberately overrides that single target path.
- `true`: the configured TeamSpeak plugin directory list and selected active target are used.

The multi-instance list is never consulted while the feature switch is disabled. This preserves the original single-instance behavior and makes migration from older configurations deterministic.
