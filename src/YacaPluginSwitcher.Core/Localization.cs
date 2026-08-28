using YacaPluginSwitcher.Core;
using System.Globalization;

namespace YacaPluginSwitcher.Configuration;

public static class Localization
{
    public const string English = "en";
    public const string German = "de";

    /// <summary>
    /// Detects the Windows-installed UI language. German is selected only for
    /// German Windows cultures; every other language intentionally falls back to English.
    /// </summary>
    public static string DetectSystemLanguage(CultureInfo? culture = null)
    {
        var systemCulture = culture ?? CultureInfo.InstalledUICulture;
        return string.Equals(systemCulture.TwoLetterISOLanguageName, German, StringComparison.OrdinalIgnoreCase)
            ? German
            : English;
    }

    public static string Normalize(string? language) =>
        string.Equals(language, German, StringComparison.OrdinalIgnoreCase) ? German : English;

    public static UiText Get(string? language) =>
        Normalize(language) == German ? UiText.German : UiText.English;

    public static string GetErrorMessage(Exception exception, UiText text, string fallback)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(text);

        return exception switch
        {
            YacaOperationException operation => operation.Code switch
            {
                AppErrorCode.InvalidYacaDll => text.ErrorInvalidYacaDll,
                AppErrorCode.TargetDirectoryMissing => text.ErrorTargetDirectoryMissing,
                AppErrorCode.BackupFailed => text.ErrorBackupFailed,
                AppErrorCode.TemporaryFileVerificationFailed => text.ErrorTemporaryVerificationFailed,
                AppErrorCode.InstalledFileVerificationFailed => text.ErrorInstalledVerificationFailed,
                AppErrorCode.BackupFileMissing => text.ErrorBackupFileMissing,
                AppErrorCode.BackupInvalid => text.ErrorBackupInvalid,
                AppErrorCode.BackupHashMismatch => text.ErrorBackupHashMismatch,
                AppErrorCode.BackupTargetDirectoryMissing => text.ErrorBackupTargetDirectoryMissing,
                AppErrorCode.ConfigurationDirectoryMissing => text.ErrorConfigurationDirectoryMissing,
                AppErrorCode.InvalidBackupDirectory => text.ErrorInvalidBackupDirectory,
                _ => fallback
            },
            UnauthorizedAccessException => text.AccessDenied,
            IOException => fallback,
            InvalidDataException => text.ErrorInvalidData,
            InvalidOperationException => fallback,
            ArgumentException => text.ErrorInvalidArgument,
            _ => text.ErrorUnexpected
        };
    }
}

public sealed record UiText(
    string Code,
    string Title,
    string Language,
    string LanguageEnglish,
    string LanguageGerman,
    string Active,
    string NotInstalled,
    string UnknownInvalid,
    string PluginsFolder,
    string Target,
    string BackupsPath,
    string TeamspeakRunning,
    string TeamspeakStopped,
    string CloseTeamspeak,
    string CloseTeamspeakQuestion,
    string CloseTeamspeakFailed,
    string Close,
    string About,
    string Backups,
    string Refresh,
    string PluginsFolderButton,
    string NoPlugins,
    string NewValidPluginFound,
    string AlreadyActiveTitle,
    string AlreadyActiveMessage,
    string TeamspeakRunningTitle,
    string TeamspeakRunningMessage,
    string SuccessTitle,
    string ActivatedMessage,
    string AccessDenied,
    string ErrorTitle,
    string OpenFolderError,
    string StartErrorTitle,
    string StartErrorMessage,
    string AboutTitle,
    string BackupTitle,
    string Restore,
    string RestoreQuestion,
    string RestoreFailed,
    string BackupRunningMessage,
    string InvalidBackup,
    string BackupCreatedBeforeRestoreFailed,
    string Date,
    string Yaca,
    string Size,
    string Hash,
    string NoBackups,
    string Community,
    string Legal,
    string GitHubRepository,
    string Config,
    string ConfigTitle,
    string Save,
    string Cancel,
    string TeamSpeakPluginPaths,
    string ActiveTeamSpeakPath,
    string AddPath,
    string RemovePath,
    string UseSelectedPath,
    string AutoDetect,
    string Browse,
    string MaxBackups,
    string AutomaticBackup,
    string WarnIfTeamSpeakRunningOption,
    string PortableNotice,
    string PathMustBeValid,
    string PathAlreadyExists,
    string DefaultPath,
    string NoTeamSpeakPaths,
    string MultipleTeamSpeakInstancesOption,
    string ExpertSettings,
    string GeneralLogging,
    string DebugLogging,
    string TeamSpeakPathWarningTitle,
    string TeamSpeakPathWarningMessage )
{
    public string ErrorInvalidYacaDll => IsGerman ? "Die ausgewählte YACA-DLL ist nicht valide." : "The selected YACA DLL is invalid.";
    public string ErrorTargetDirectoryMissing => IsGerman ? "Das Zielverzeichnis konnte nicht bestimmt werden." : "The target directory could not be determined.";
    public string ErrorBackupFailed => IsGerman ? "Die bestehende YACA-DLL konnte vor dem Wechsel nicht gesichert werden. Der Wechsel wurde abgebrochen." : "The existing YACA DLL could not be backed up before switching. The operation was cancelled.";
    public string ErrorTemporaryVerificationFailed => IsGerman ? "Die temporäre Installationsdatei konnte nicht verifiziert werden." : "The temporary installation file could not be verified.";
    public string ErrorInstalledVerificationFailed => IsGerman ? "Die installierte Zieldatei konnte nach dem Wechsel nicht verifiziert werden." : "The installed target file could not be verified after switching.";
    public string ErrorBackupFileMissing => IsGerman ? "Die Backup-Datei wurde nicht gefunden." : "The backup file was not found.";
    public string ErrorBackupInvalid => IsGerman ? "Das Backup ist keine gültige YACA-DLL." : "The backup is not a valid YACA DLL.";
    public string ErrorBackupHashMismatch => IsGerman ? "Die SHA-256-Prüfung des Backups ist fehlgeschlagen." : "Backup SHA-256 verification failed.";
    public string ErrorBackupTargetDirectoryMissing => IsGerman ? "Das Zielverzeichnis der TeamSpeak-3-Installation konnte nicht bestimmt werden." : "The TeamSpeak 3 target directory could not be determined.";
    public string ErrorConfigurationDirectoryMissing => IsGerman ? "Das Konfigurationsverzeichnis konnte nicht bestimmt werden." : "The configuration directory could not be determined.";
    public string ErrorInvalidData => IsGerman ? "Die empfangenen oder ausgewählten Daten sind ungültig." : "The selected or received data is invalid.";
    public string ErrorInvalidArgument => IsGerman ? "Ungültige Eingabe." : "Invalid input.";
    public string ErrorUnexpected => IsGerman ? "Ein unerwarteter Fehler ist aufgetreten." : "An unexpected error occurred.";
    public bool IsGerman => string.Equals(Code, Localization.German, StringComparison.OrdinalIgnoreCase);
    public string AlreadyRunningMessage => IsGerman ? "YACA Plugin Switcher läuft bereits." : "YACA Plugin Switcher is already running.";
    public string TechnicalDetails => IsGerman ? "Technische Details" : "Technical details";
    public string StartupInitializing => IsGerman ? "Programm wird gestartet..." : "Starting application...";
    public string StartupStarting => IsGerman ? "YACA TeamSpeak 3 Plugin Switcher wird gestartet..." : "Starting YACA TeamSpeak 3 Plugin Switcher...";
    public string StartupLoading => IsGerman ? "Konfiguration und Plugins werden initialisiert..." : "Initializing configuration and plugins...";
    public string StartupReady => IsGerman ? "Bereit. Hauptfenster wird geöffnet..." : "Ready. Opening main window...";
    public string LinkOpenFailed => IsGerman ? "Der Link konnte nicht geöffnet werden." : "The link could not be opened.";
    public string LinkTitle => IsGerman ? "Link" : "Link";
    public string Delete => IsGerman ? "Löschen" : "Delete";
    public string DeleteBackups => IsGerman ? "Backups löschen" : "Delete Backups";
    public string DeleteBackupsQuestion => IsGerman ? "Möchtest du die ausgewählten Backups wirklich löschen?" : "Do you really want to delete the selected backups?";
    public string DeleteAllBackupsQuestion => IsGerman ? "Möchtest du wirklich ALLE Backups löschen?" : "Do you really want to delete ALL backups?";
    public string NoBackupsSelected => IsGerman ? "Bitte markiere mindestens ein Backup zum Löschen." : "Please select at least one backup for deletion.";
    public string SelectableBackups => IsGerman ? "Einzelne Backups zum Löschen markierbar machen" : "Selectable Backups for deletion";
    public string ErrorInvalidBackupDirectory => IsGerman ? "Das ausgewählte Backup-Verzeichnis ist ungültig." : "The selected backup directory is invalid.";

    public static UiText English { get; } = new(
        Localization.English,
        "YACA Plugin Switcher (by ViP3R_76)",
        "Language",
        "English",
        "Deutsch",
        "Currently installed:",
        "[!] Not installed",
        "[!] Unknown / invalid YACA DLL",
        "Plugins:",
        "Target:",
        "Backups:",
        "[!] TeamSpeak 3 is currently running - close it before switching.",
        "[OK] TeamSpeak 3 is not running.",
        "Close TeamSpeak 3",
        "TeamSpeak 3 is currently running.\n\nDo you really want to close TeamSpeak 3 now?",
        "TeamSpeak 3 could not be closed completely. Please close it manually before switching YACA.",
        "Close",
        "About",
        "Backups",
        "Refresh",
        "Plugins Folder",
        "No valid YACA DLLs found in the local Plugins folder.",
        "New valid YACA DLL found: {0}",
        "Already active",
        "is already active.\n\nNo file was changed.",
        "TeamSpeak 3 is running",
        "TeamSpeak 3 is currently running. The active YACA DLL may still be in use.\n\nRecommended: close TeamSpeak completely.\n\nTry anyway?",
        "Successfully activated",
        "was successfully activated.\n\nFile: yaca_win64.dll\nSHA-256:",
        "Access denied. Make sure TeamSpeak is closed and you have write permissions for the TeamSpeak plugin directory.",
        "YACA Switcher - Error",
        "The local Plugins folder could not be opened.",
        "YACA Plugin Switcher - Startup Error",
        "YACA Plugin Switcher could not be started.",
        "About - YACA Plugin Switcher",
        "YACA Backups",
        "Restore",
        "Restore backup?",
        "Restore failed",
        "TeamSpeak 3 is running. Please close TeamSpeak completely before restoring a backup.",
        "The selected backup information is invalid.",
        "The currently installed YACA DLL could not be backed up before restoring.",
        "Date",
        "YACA",
        "Size",
        "SHA-256",
        "No backups available.",
        "COMMUNITY",
        "LEGAL",
        "GitHub Repository",
        "Configuration",
        "YACA Plugin Switcher - Configuration",
        "Save",
        "Cancel",
        "TeamSpeak 3 plugin locations",
        "Active TeamSpeak 3 plugin path",
        "Add path",
        "Remove",
        "Use selected",
        "Auto-detect",
        "Browse...",
        "Maximum backups",
        "Create automatic backup before switching",
        "Warn when TeamSpeak 3 is running",
        "Portable mode: config.json and logs are stored beside the executable.",
        "The selected path is invalid.",
        "This path is already in the list.",
        "Default / detected",
        "No TeamSpeak 3 plugin paths configured.",
        "Use multiple TeamSpeak 3 installations (advanced)",
        "Expert settings",
        "General logging",
        "Debug logging",
        "TeamSpeak 3 plugin path warning",
        "No yaca_win64.dll was found in the selected folder. This may indicate that the wrong folder was selected or that YACA is not installed yet. Do you want to save this path anyway?");

    public static UiText German { get; } = new(
        Localization.German,
        "YACA Plugin Switcher (by ViP3R_76)",
        "Sprache",
        "English",
        "Deutsch",
        "Aktuell installiert:",
        "[!] Nicht installiert",
        "[!] Unbekannte / ungültige YACA-DLL",
        "Plugins:",
        "Ziel:",
        "Backups:",
        "[!] TeamSpeak 3 läuft derzeit - vor einem Wechsel schließen.",
        "[OK] TeamSpeak 3 ist nicht gestartet.",
        "TeamSpeak 3 schließen",
        "TeamSpeak 3 läuft derzeit.\n\nMöchtest du TeamSpeak 3 jetzt wirklich schließen?",
        "TeamSpeak 3 konnte nicht vollständig geschlossen werden. Bitte schließe TeamSpeak manuell, bevor du YACA wechselst.",
        "Schließen",
        "Info",
        "Backups",
        "Aktualisieren",
        "Plugins-Ordner",
        "Keine gültigen YACA-DLLs im lokalen Plugins-Ordner gefunden.",
        "Neue valide YACA-DLL gefunden: {0}",
        "Bereits aktiv",
        "ist bereits aktiv.\n\nEs wurde keine Datei verändert.",
        "TeamSpeak 3 läuft",
        "TeamSpeak 3 läuft derzeit. Die aktive YACA-DLL könnte noch verwendet werden.\n\nEmpfohlen: TeamSpeak vollständig schließen.\n\nTrotzdem versuchen?",
        "Erfolgreich aktiviert",
        "wurde erfolgreich aktiviert.\n\nDatei: yaca_win64.dll\nSHA-256:",
        "Zugriff verweigert. Stelle sicher, dass TeamSpeak geschlossen ist und du Schreibrechte für das TeamSpeak-Plugin-Verzeichnis besitzt.",
        "YACA Switcher - Fehler",
        "Der lokale Plugins-Ordner konnte nicht geöffnet werden.",
        "YACA Plugin Switcher - Startfehler",
        "YACA Plugin Switcher konnte nicht gestartet werden.",
        "Info - YACA Plugin Switcher",
        "YACA Backups",
        "Wiederherstellen",
        "Backup wiederherstellen?",
        "Wiederherstellung fehlgeschlagen",
        "TeamSpeak 3 läuft. Bitte TeamSpeak vollständig schließen, bevor ein Backup wiederhergestellt wird.",
        "Die ausgewählte Backup-Information ist ungültig.",
        "Die aktuell installierte YACA-DLL konnte vor der Wiederherstellung nicht gesichert werden.",
        "Datum",
        "YACA",
        "Größe",
        "SHA-256",
        "Keine Backups vorhanden.",
        "COMMUNITY",
        "RECHTLICHES",
        "GitHub Repository",
        "Konfiguration",
        "YACA Plugin Switcher - Konfiguration",
        "Speichern",
        "Abbrechen",
        "TeamSpeak-3-Pluginpfade",
        "Aktiver TeamSpeak-3-Pluginpfad",
        "Pfad hinzufügen",
        "Entfernen",
        "Auswahl verwenden",
        "Automatisch erkennen",
        "Durchsuchen...",
        "Maximale Backups",
        "Automatisches Backup vor dem Wechsel erstellen",
        "Warnen, wenn TeamSpeak 3 läuft",
        "Portabler Modus: config.json und Logs werden neben der EXE gespeichert.",
        "Der ausgewählte Pfad ist ungültig.",
        "Dieser Pfad ist bereits in der Liste.",
        "Standard / erkannt",
        "Keine TeamSpeak-3-Pluginpfade konfiguriert.",
        "Mehrere TeamSpeak-3-Installationen verwenden (Erweitert)",
        "Experten-Einstellungen",
        "Allgemeines Logging",
        "Debug-Logging",
        "Warnung zum TeamSpeak-3-Pluginpfad",
        "Im ausgewählten Ordner wurde keine yaca_win64.dll gefunden. Das kann bedeuten, dass der falsche Ordner ausgewählt wurde oder YACA noch nicht installiert ist. Möchtest du diesen Pfad trotzdem speichern?");
}
