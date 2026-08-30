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
    public int MaxBackups { get; set; } = 4;
    public bool AutomaticBackup { get; set; } = true;
    public bool WarnIfTeamSpeakRunning { get; set; } = true;
    public bool ExpertSettings { get; set; }
    public bool GeneralLogging { get; set; } = true;
    public bool DebugLogging { get; set; }
    public bool SelectableBackupsForDeletion { get; set; }
    public bool KeepYacaPluginDownloads { get; set; }
    public bool DownloadAllPluginsWithoutPrompt { get; set; }
    public string Language { get; set; } = string.Empty;

    [JsonIgnore]
    public string SettingsFilePath { get; private set; } = string.Empty;

    public static AppSettings Load(string settingsFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);

        try
        {
            if (File.Exists(settingsFilePath))
            {
                var json = File.ReadAllText(settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                settings.SettingsFilePath = Path.GetFullPath(settingsFilePath);
                settings.Normalize();
                return settings;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
        }

        return new AppSettings
        {
            SettingsFilePath = Path.GetFullPath(settingsFilePath),
            Language = Localization.DetectSystemLanguage()
        };
    }

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