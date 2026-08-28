namespace YacaPluginSwitcher.Models;

public sealed record ValidationResult(
    bool IsValid,
    string Message,
    Version? Version = null,
    long? Build = null,
    long FileSize = 0,
    string? Sha256 = null);
