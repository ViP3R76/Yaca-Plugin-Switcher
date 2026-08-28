# Release Checklist

## Local release validation

- [ ] Confirm `Version` in `Directory.Build.props`.
- [ ] Run `dotnet restore YacaPluginSwitcher.sln`.
- [ ] Run `dotnet build YacaPluginSwitcher.sln -c Release --no-restore`.
- [ ] Run `./scripts/publish-release.ps1 -Configuration Release`.
- [ ] Confirm output is under `<solution root>/releases/`.
- [ ] Confirm the ZIP contains exactly one `.exe`.
- [ ] Confirm the ZIP contains no `.pdb`, `.dll`, `.deps.json`, or `.runtimeconfig.json`.
- [ ] Confirm the generated `.sha256` matches the ZIP.
- [ ] Test the EXE on a clean Windows 10/11 system with no .NET Desktop Runtime installed.
- [ ] Test first start, TeamSpeak detection, plugin scan, backup/restore, configuration, language selection and graceful TeamSpeak close.

## GitHub release

Create and push a version tag, for example:

```text
git tag v1.0.0
git push origin v1.0.0
```

The GitHub Actions workflow builds the self-contained single-file release, validates the package, creates the SHA-256 file and publishes the tagged GitHub Release.

## Code signing

Code signing is intentionally not required yet. When a trusted Authenticode certificate is available, signing can be inserted before ZIP creation without changing the application itself.
