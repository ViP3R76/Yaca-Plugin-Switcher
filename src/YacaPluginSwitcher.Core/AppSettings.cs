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
    /// <summary>When false, only the automatically detected/configured single TS3 instance is used.</summary>
    public bool UseMultipleTeamSpeakInstances { get; set; }
    /// <summary>When single-instance mode is active, true means the user explicitly selected a custom target path.</summary>
    public bool UseCustomTeamSpeakPluginDirectory { get; set; }
    public int MaxBackups { get; set; } = 4;
    public bool AutomaticBackup { get; set; } = true;
    public bool WarnIfTeamSpeakRunning { get; set; } = true;
    /// <summary>Shows expert-only configuration options. New installations keep this disabled.</summary>
    public bool ExpertSettings { get; set; }
    /// <summary>Enables normal informational logging. Warnings and errors remain available.</summary>
    public bool GeneralLogging { get; set; } = true;
    /// <summary>Enables verbose diagnostic logging.</summary>
    public bool DebugLogging { get; set; }
    /// <summary>Allows individual backup entries to be selected for deletion.</summary>
    public bool SelectableBackupsForDeletion { get; set; }
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
            // Startup must remain possible even when the portable config is damaged or inaccessible.
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
                // Cleanup must not mask the original save error.
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
