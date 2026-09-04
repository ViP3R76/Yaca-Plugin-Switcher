global using System.Text;

using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.Core;

public sealed class YacaService
{
    public const string TargetFileName = "yaca_win64.dll";

    public AppPaths Paths { get; }
    public AppSettings Settings { get; }
    public Logger Logger { get; }
    public YacaScanner Scanner { get; }
    public BackupManager Backups { get; }
    public YacaInstaller Installer { get; }

    public string TargetDirectory
    {
        get
        {
            var configured = Settings.TeamSpeakPluginDirectory;
            if (Settings.UseMultipleTeamSpeakInstances && !string.IsNullOrWhiteSpace(configured))
                return configured;

            if (!Settings.UseMultipleTeamSpeakInstances
                && Settings.UseCustomTeamSpeakPluginDirectory
                && !string.IsNullOrWhiteSpace(configured))
            {
                return configured;
            }

            return GetDefaultTeamSpeakPluginDirectory();
        }
    }

    public string TargetFile => Path.Combine(TargetDirectory, TargetFileName);

    public YacaService()
    {
        Paths = new AppPaths(AppContext.BaseDirectory);
        Paths.EnsureDirectories();
        Settings = AppSettings.Load(Paths.SettingsFilePath);
        InitializeTeamSpeakPathSettings();
        Logger = new Logger(Paths.LogDirectory, Settings.GeneralLogging, Settings.DebugLogging);
        Scanner = new YacaScanner(Logger);
        Backups = new BackupManager(Paths.BackupDirectory, Logger);
        Installer = new YacaInstaller(Backups, Logger);
    }

    public IReadOnlyList<YacaPluginInfo> ScanPlugins() => Scanner.Scan(Paths.PluginDirectory);

    public YacaPluginInfo? DetectCurrent()
    {
        if (!File.Exists(TargetFile))
            return null;

        var validation = YacaValidator.Validate(TargetFile);
        if (!validation.IsValid || validation.Version is null || string.IsNullOrWhiteSpace(validation.Sha256))
            return null;

        return new YacaPluginInfo(TargetFile, TargetFileName, validation.Version, validation.Build, validation.FileSize, validation.Sha256, true, validation.Message);
    }

    public void SetTargetDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var fullPath = Path.GetFullPath(directory.Trim());
        Directory.CreateDirectory(fullPath);
        Settings.AddTeamSpeakPluginDirectory(fullPath);
        Settings.TeamSpeakPluginDirectory = fullPath;
        Settings.UseCustomTeamSpeakPluginDirectory = true;
        Settings.Save();
    }

    public static string GetDefaultTeamSpeakPluginDirectory()
    {
        var candidates = GetTeamSpeakPluginDirectoryCandidates();
        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    public static IReadOnlyList<string> GetTeamSpeakPluginDirectoryCandidates()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(appData))
        {
            candidates.Add(Path.Combine(appData, "TS3Client", "plugins"));
            candidates.Add(Path.Combine(appData, "TeamSpeak 3 Client", "plugins"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
            candidates.Add(Path.Combine(localAppData, "TS3Client", "plugins"));

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void InitializeTeamSpeakPathSettings()
    {
        var defaultPath = GetDefaultTeamSpeakPluginDirectory();

        if (Settings.UseMultipleTeamSpeakInstances)
        {
            Settings.AddTeamSpeakPluginDirectory(defaultPath);
            if (!string.IsNullOrWhiteSpace(Settings.TeamSpeakPluginDirectory))
                Settings.AddTeamSpeakPluginDirectory(Settings.TeamSpeakPluginDirectory);
        }

        try
        {
            Settings.Save();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
