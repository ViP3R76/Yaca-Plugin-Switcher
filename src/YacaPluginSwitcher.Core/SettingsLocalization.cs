namespace YacaPluginSwitcher.Configuration;

/// <summary>
/// Zentrale Texte der Einstellungen und kleiner datenorientierter UI-Tabellen.
/// UI-spezifische Bezeichnungen bleiben außerhalb der View-XAML und werden abhängig von der aktiven Sprache geliefert.
/// </summary>
public static class SettingsLocalization
{
    private static readonly Dictionary<string, (string German, string English)> Texts =
        new(StringComparer.Ordinal)
        {
            ["Configuration"] = ("KONFIGURATION", "CONFIGURATION"),
            ["General"] = ("ALLGEMEIN", "GENERAL"),
            ["Language"] = ("Sprache", "Language"),
            ["YacaDownloader"] = ("YACA DOWNLOADER", "YACA DOWNLOADER"),
            ["KeepDownloads"] = ("Yaca Plugin Downloads behalten", "Keep Yaca plugin downloads"),
            ["DownloadAll"] = ("Alle Plugins direkt downloaden, ohne Nachfrage", "Download all plugins directly without prompting"),
            ["TeamSpeak"] = ("TEAMSPEAK", "TEAMSPEAK"),
            ["ActiveTeamSpeakPath"] = ("Aktiver TeamSpeak-Pfad", "Active TeamSpeak path"),
            ["TeamSpeakPathTooltip"] = ("Vollständiger TeamSpeak Plugin-Pfad", "Full TeamSpeak plugin path"),
            ["Browse"] = ("Durchsuchen", "Browse"),
            ["MultipleInstances"] = ("Mehrere TeamSpeak-3-Installationen verwenden (Experten-Einstellungen)", "Use multiple TeamSpeak 3 installations (Expert settings)"),
            ["ExpertSettings"] = ("Experten-Einstellungen", "Expert settings"),
            ["TeamSpeakInstances"] = ("TEAMSPEAK INSTANZEN", "TEAMSPEAK INSTANCES"),
            ["AvailableTeamSpeakPaths"] = ("Verfügbare TeamSpeak-Pfade", "Available TeamSpeak paths"),
            ["AddPath"] = ("Hinzufügen", "Add"),
            ["Remove"] = ("Entfernen", "Remove"),
            ["UseSelectedPath"] = ("Ausgewählten Pfad verwenden", "Use selected path"),
            ["AutoDetect"] = ("Auto-Erkennung", "Auto-detect"),
            ["LoggingBackups"] = ("PROTOKOLLIERUNG & BACKUPS", "LOGGING & BACKUPS"),
            ["LogDirectory"] = ("Log-Verzeichnis", "Log directory"),
            ["Open"] = ("Öffnen", "Open"),
            ["DeleteLogs"] = ("Logs löschen?", "Delete logs?"),
            ["Delete"] = ("LÖSCHEN", "DELETE"),
            ["Cancel"] = ("ABBRECHEN", "CANCEL"),
            ["ApplicationDirectories"] = ("ANWENDUNGSVERZEICHNISSE", "APPLICATION DIRECTORIES"),
            ["Backups"] = ("Backups", "Backups"),
            ["Plugins"] = ("Plugins", "Plugins"),
            ["ApplicationDirectory"] = ("App-Verzeichnis", "Application directory"),
            ["Save"] = ("Speichern", "Save"),
            ["PendingChanges"] = ("Änderungen ausstehend – bitte speichern", "Pending changes – please save"),
            ["MaximumBackups"] = ("Maximale Backups", "Maximum backups"),
            ["AutomaticBackup"] = ("Automatisches Backup vor dem Wechsel erstellen", "Create automatic backup before switching"),
            ["WarnRunning"] = ("Warnen, wenn TeamSpeak 3 läuft", "Warn when TeamSpeak 3 is running"),
            ["SelectableBackups"] = ("Einzelne Backups zum Löschen markierbar machen", "Allow individual backups to be selected for deletion"),
            ["BackupReplacementNotice"] = ("Älteste Backups werden ersetzt", "Oldest backups are replaced"),
            ["BackupColumn"] = ("Backup", "Backup"),
            ["FileColumn"] = ("Datei", "File"),
            ["DateColumn"] = ("Datum", "Date"),
            ["SizeColumn"] = ("Größe", "Size"),
            ["HashColumn"] = ("SHA-256", "SHA-256"),
            ["VersionColumn"] = ("Version", "Version")
        };

    public static string Get(string? language, string key)
    {
        if (!Texts.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Unknown settings localization key: {key}");

        return Localization.Normalize(language) == Localization.German
            ? value.German
            : value.English;
    }
}
