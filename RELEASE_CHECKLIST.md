# Release Checklist

- Confirm Version in Directory.Build.props.
- Build Release | x64 on Windows with .NET 10.
- Confirm the user ZIP contains exactly one EXE and no PDB, DLL, deps.json or runtimeconfig.json.
- Confirm SHA-256 checksum.
- Test first start, TeamSpeak detection, plugin scan, backup/restore, configuration, language selection and TeamSpeak close.

## GitHub

A tag matching `vMAJOR.MINOR.PATCH` triggers the automated release workflow. It builds, validates, checksums and publishes the ready-to-use ZIP to the GitHub Release.
