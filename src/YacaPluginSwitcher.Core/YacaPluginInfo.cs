namespace YacaPluginSwitcher.Models;

public sealed record YacaPluginInfo(
    string FilePath,
    string FileName,
    Version Version,
    long? Build,
    long FileSize,
    string Sha256,
    bool IsValid,
    string? ValidationMessage = null)
{
    public string DisplayName => Build.HasValue
        ? $"YACA {Version} (Build: {Build.Value})"
        : $"YACA {Version}";
}
