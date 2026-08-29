namespace YacaPluginSwitcher.Models;

public sealed record BackupInfo(
    string Directory,
    DateTime Timestamp,
    string FileName,
    string SourceDisplayName,
    long FileSize,
    string Sha256)
{
    public string DisplayName => SourceDisplayName;
    public string? SourceVersion { get; init; }
    public long? SourceBuild { get; init; }
    public bool IsAutomatic { get; init; }
}
