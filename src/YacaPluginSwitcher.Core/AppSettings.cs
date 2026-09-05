using YacaPluginSwitcher.Core;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YacaPluginSwitcher.Configuration;

public sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string? TeamSpeakPluginDirectory { get; set; }
    public List<string> TeamSpeakPluginDirectories { get; set; } = [];
    public bool UseMultipleTeamSpeakInstances { get; set; }
    public bool UseCustomTeamSpeakPluginDirectory { get; set; }
    public int MaxBackups { get; set; }
    public bool AutomaticBackup { get; set; }
    public bool WarnIfTeamSpeakRunning { get; set; }
    public bool ExpertSettings { get; set; }
    public bool GeneralLogging { get; set; }
    public bool DebugLogging { get; set; }
    public bool SelectableBackupsForDeletion { get; set; }
    public bool KeepYacaPluginDownloads { get; set; }
    public bool DownloadAllPluginsWithoutPrompt { get; set; }
    public string Language { get; set; } = string.Empty;

    [JsonIgnore]
    public string SettingsFilePath { get; private set; } = string.Empty;

    [JsonIgnore]
    public bool IsFirstRun { get; private set; }

    public static AppSettings Load(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);
        var fullPath = Path.GetFullPath(settingsFilePath);

        if (File.Exists(fullPath))
        {
            try
            {
                var json = File.ReadAllText(fullPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                settings.SettingsFilePath = fullPath;
                settings.IsFirstRun = false;
                settings.Normalize();
                return settings;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
            {
                // An existing configuration is never replaced by first-run defaults.
                // Keep the legacy load-failure behavior without marking it as first run.
                return new AppSettings
                {
                    SettingsFilePath = fullPath,
                    IsFirstRun = false,
                    Language = Localization.DetectSystemLanguage()
                };
            }
        }

        return CreateFirstRunDefaults(fullPath);
    }

    private static AppSettings CreateFirstRunDefaults(string settingsFilePath) => new()
    {
        SettingsFilePath = settingsFilePath,
        IsFirstRun = true,
        MaxBackups = 4,
        AutomaticBackup = true,
        WarnIfTeamSpeakRunning = true,
        KeepYacaPluginDownloads = false,
        DownloadAllPluginsWithoutPrompt = false,
        UseMultipleTeamSpeakInstances = false,
        ExpertSettings = false,
        GeneralLogging = false,
        DebugLogging = false,
        SelectableBackupsForDeletion = false,
        UseCustomTeamSpeakPluginDirectory = false,
        Language = Localization.DetectSystemLanguage()
    };

    public void Save()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(SettingsFilePath);
        Normalize();

        var directory = Path.GetDirectoryName(SettingsFilePath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new YacaOperationException(AppErrorCode.ConfigurationDirectoryMissing, "Configuration directory unavailable.");

        Directory.CreateDirectory(directory);
        var temporaryPath = SettingsFilePath + ".tmp";
        var json = JsonSerializer.Serialize(this, JsonOptions);

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, SettingsFilePath, true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
            }
        }
    }

    public void AddTeamSpeakPluginDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var fullPath = Path.GetFullPath(directory.Trim());
        if (!TeamSpeakPluginDirectories.Any(path => string.Equals(path, fullPath, StringComparison.OrdinalIgnoreCase)))
            TeamSpeakPluginDirectories.Add(fullPath);
    }

    public void RemoveTeamSpeakPluginDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return;

        TeamSpeakPluginDirectories.RemoveAll(path => string.Equals(path, directory, StringComparison.OrdinalIgnoreCase));
        if (string.Equals(TeamSpeakPluginDirectory, directory, StringComparison.OrdinalIgnoreCase))
            TeamSpeakPluginDirectory = null;
    }

    private void Normalize()
    {
        if (MaxBackups < 1 || MaxBackups > 9)
            MaxBackups = 4;

        Language = string.IsNullOrWhiteSpace(Language)
            ? Localization.DetectSystemLanguage()
            : Localization.Normalize(Language);

        TeamSpeakPluginDirectories ??= [];
        var normalized = TeamSpeakPluginDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path =>
            {
                try { return Path.GetFullPath(path.Trim()); }
                catch (ArgumentException) { return null; }
                catch (NotSupportedException) { return null; }
            })
            .Where(path => path is not null)
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        TeamSpeakPluginDirectories = normalized;

        if (!string.IsNullOrWhiteSpace(TeamSpeakPluginDirectory))
        {
            try
            {
                TeamSpeakPluginDirectory = Path.GetFullPath(TeamSpeakPluginDirectory.Trim());
                AddTeamSpeakPluginDirectory(TeamSpeakPluginDirectory);
            }
            catch (ArgumentException)
            {
                TeamSpeakPluginDirectory = null;
            }
            catch (NotSupportedException)
            {
                TeamSpeakPluginDirectory = null;
            }
        }
    }
}
