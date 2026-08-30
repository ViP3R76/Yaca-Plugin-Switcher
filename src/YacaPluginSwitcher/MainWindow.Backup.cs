using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Erstellt aus dem Dashboard heraus ein manuelles Backup.
    /// </summary>
    private void CreateBackupFromDashboard()
    {
        var text = Texts;

        if (TeamSpeakDetector.IsRunning()
            && _service.Settings.WarnIfTeamSpeakRunning)
        {
            SetGlobalStatus(text.TeamspeakRunningMessage);
            GlobalFooterStatusText.Foreground =
                (Brush)FindResource("ErrorBrush");
            GlobalFooterStatusText.FontWeight = FontWeights.Bold;
            return;
        }

        try
        {
            var current = _service.DetectCurrent();

            if (current is null)
            {
                ShowError(text.NotInstalled);
                return;
            }

            if (_service.Backups.CreateBackup(
                    _service.TargetFile,
                    current,
                    automatic: false) is null)
            {
                ShowError(text.ErrorUnexpected);
                return;
            }

            _service.Backups.Trim(_service.Settings.MaxBackups);
            RefreshHome();
            SetGlobalStatus(
                IsGerman
                    ? "Backup wurde erfolgreich erstellt."
                    : "Backup created successfully.",
                true);
        }
        catch (Exception ex) when (
            ex is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or InvalidOperationException)
        {
            _service.Logger.Error($"Dashboard backup failed: {ex}");
            SetGlobalStatus(
                Localization.GetErrorMessage(
                    ex,
                    text,
                    text.ErrorUnexpected));
        }
    }

    /// <summary>
    /// Öffnet die Backupverwaltung.
    /// </summary>
    private void ShowBackups()
    {
        _activePage = "backups";
        SetActiveNav("backups");
        PageHost.Content = new BackupView(_service, this);
        SetGlobalStatus(
            IsGerman
                ? "Backupverwaltung geöffnet."
                : "Backup management opened.");
    }
}
