# Security Policy

## Reporting

Please do not publish security-sensitive information in a public issue. Use a private security channel if one is configured for the repository.

For ordinary plugin validation or installation problems, open a GitHub issue with a sanitized log and the detected YACA version/build.

## Design notes

The switcher does not load YACA DLLs into its own process. It reads them as binary files, validates their PE structure/signatures, computes a SHA-256 hash and copies them only after validation.

Production releases are self-contained Windows x64 single-file executables and are validated by the release workflow before publication.
