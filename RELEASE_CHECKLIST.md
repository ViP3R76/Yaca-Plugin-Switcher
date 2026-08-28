# Release Checklist

## Local release validation
- [ ] Confirm `Version` in `Directory.Build.props`.
- [ ] Run `dotnet restore YacaPluginSwitcher.sln`.
- [ ] Run `dotnet build YacaPluginSwitcher.sln -c Release --no-restore`.
- [ ] Confirm the user ZIP contains exactly one `.exe` and no `.pdb`, `.dll`, `.deps.json`, or `.runtimeconfig.json`.
- [ ] Confirm the generated SHA-256 checksum.
- [ ] Test the EXE on clean Windows 10/11 x64.

## GitHub release
A tag matching `vMAJOR.MINOR.PATCH` triggers the automated Windows x64 release workflow. The workflow builds, validates, checksums and publishes the ready-to-use ZIP to the GitHub Release.

Manual workflow execution is also available from the Actions tab.

## Code signing
Code signing is intentionally not required yet. It can be inserted before ZIP creation when a trusted Authenticode certificate is available.
