namespace YacaPluginSwitcher.Configuration;

public sealed class AppPaths
{
    public string BaseDirectory { get; }
    public string PluginDirectory { get; }
    public string DataDirectory { get; }
    public string BackupDirectory { get; }
    public string LogDirectory { get; }
    public string TempDirectory { get; }
    public string SettingsFilePath { get; }

    public AppPaths(string baseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);

        BaseDirectory = Path.GetFullPath(baseDirectory);
        PluginDirectory = Path.Combine(BaseDirectory, "Plugins");
        DataDirectory = BaseDirectory;
        BackupDirectory = Path.Combine(BaseDirectory, "Backups");
        LogDirectory = Path.Combine(BaseDirectory, "Logs");
        TempDirectory = Path.Combine(BaseDirectory, "temp");
        SettingsFilePath = Path.Combine(BaseDirectory, "config.json");
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(PluginDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(TempDirectory);
    }
}
