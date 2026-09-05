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

    public bool EnsureCurrentPluginAvailable()
    {
        var current = DetectCurrent();
        if (current is null)
            return false;

        var existing = ScanPlugins().FirstOrDefault(plugin =>
            plugin.Version == current.Version
            && plugin.Sha256.Equals(current.Sha256, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            return false;

        Directory.CreateDirectory(Paths.PluginDirectory);
        var tag = current.Version.ToString().Replace(".", string.Empty, StringComparison.Ordinal);
        var target = Path.Combine(Paths.PluginDirectory, $"yaca_{tag}_win64.dll");
        var staged = Path.Combine(Paths.PluginDirectory, $".yaca_protect_{Guid.NewGuid():N}.tmp");

        try
        {
            File.Copy(TargetFile, staged, true);
            var stagedValidation = YacaValidator.Validate(staged);
            if (!stagedValidation.IsValid
                || stagedValidation.Version != current.Version
                || !string.Equals(stagedValidation.Sha256, current.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Die installierte YACA-DLL konnte nicht sicher in den Plugins-Ordner übernommen werden.");
            }

            File.Move(staged, target, true);

            var finalValidation = YacaValidator.Validate(target);
            if (!finalValidation.IsValid
                || finalValidation.Version != current.Version
                || !string.Equals(finalValidation.Sha256, current.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                TryDelete(target);
                throw new InvalidDataException("Die geschützte YACA-DLL konnte nach dem Kopieren nicht validiert werden.");
            }

            Logger.Info($"Installierte YACA-Version geschützt: {current.Version} -> {target}");
            return true;
        }
        finally
        {
            TryDelete(staged);
        }
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

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
