# YACA Detection

The current detector uses several independent characteristics observed in the supplied YACA Windows x64 binaries:

- PE `MZ` and `PE` signatures
- AMD64 machine type (`0x8664`)
- DLL image characteristic (`0x2000`)
- `Yaca Voice`
- `yaca systems`
- `fetchCurrentPluginVersion`
- `checkRequiredVersion`
- embedded `major.minor.patch.build` version string

At least three of the four YACA-specific signatures must be present. The detector then requires a valid four-component version with a numeric build component.

This is intentionally content-based and filename-independent. If YACA changes its internal binary format in a future release, the detector may need a new recognition rule.
