using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Zeigt das Dashboard an und aktualisiert dessen Daten.
    /// </summary>
    private void ShowHome()
    {
        _activePage = "home";
        PageHost.Content = RenderDashboard();
        SetActiveNav("home");
        SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
        RefreshHome();
    }

    /// <summary>
    /// Aktualisiert die im Dashboard angezeigten Plugin-, TeamSpeak- und Backupdaten.
    /// </summary>
    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home")
        {
            return;
        }

        try
        {
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());

            var current = _service.DetectCurrent();
            UpdateCurrentInstalled(current);

            var teamSpeakRunning = TeamSpeakDetector.IsRunning();

            if (_tsStatus is not null)
            {
                _tsStatus.Text = teamSpeakRunning
                    ? (IsGerman ? "GESTARTET" : "RUNNING")
                    : (IsGerman ? "NICHT GESTARTET" : "NOT RUNNING");

                _tsStatus.Foreground = teamSpeakRunning
                    ? (Brush)FindResource("ErrorBrush")
                    : (Brush)FindResource("SuccessBrush");
            }

            if (_tsDescription is not null)
            {
                _tsDescription.Text = teamSpeakRunning
                    ? (IsGerman ? "TeamSpeak 3 ist aktiv!" : "TeamSpeak 3 is active!")
                    : (IsGerman ? "TeamSpeak 3 ist nicht aktiv!" : "TeamSpeak 3 is not active!");
            }

            if (_tsInstruction is not null)
            {
                _tsInstruction.Text = teamSpeakRunning
                    ? (IsGerman
                        ? "Wechsel nur bei geschlossenem TeamSpeak möglich"
                        : "Switching is only possible when TeamSpeak is closed")
                    : (IsGerman
                        ? "Wechsel jederzeit möglich"
                        : "Switching is ready");
            }

            if (_tsClose is not null)
            {
                // A stopped TeamSpeak must not reserve the close-button's space;
                // otherwise the remaining status content is visually shifted upward.
                _tsClose.Visibility = teamSpeakRunning
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            UpdateBackupSummary(_service.Backups.ListBackups().FirstOrDefault());
            RenderVersionList(current);

            if (announce)
            {
                SetGlobalStatus(
                    teamSpeakRunning
                        ? Texts.TeamspeakRunning
                        : Texts.TeamspeakStopped);
            }
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Dashboard refresh failed: {ex}");
            ShowError(Texts.ErrorUnexpected);
        }
    }

    /// <summary>
    /// Aktualisiert die Anzeige des aktuell installierten YACA-Plugins.
    /// </summary>
    private void UpdateCurrentInstalled(YacaPluginInfo? current)
    {
        if (_currentValue is null)
        {
            return;
        }

        _currentValue.Text = current?.Version?.ToString()
            ?? (File.Exists(_service.TargetFile)
                ? Texts.UnknownInvalid
                : Texts.NotInstalled);

        _currentValue.Foreground = current is null
            ? (Brush)FindResource("WarningBrush")
            : (Brush)FindResource("ForegroundBrush");

        if (_currentDetails is null)
        {
            return;
        }

        var sizeCulture = IsGerman ? CultureInfo.GetCultureInfo("de-DE") : CultureInfo.GetCultureInfo("en-US");
        var sizeLabel = IsGerman ? "Größe" : "Size";
        _currentDetails.Text = current is null
            ? string.Empty
            : $"Build: YACA {current.Version} - " +
              $"{current.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"}\n" +
              $"{sizeLabel}: {current.FileSize.ToString("N0", sizeCulture)} Bytes\n" +
              "SHA-256\n" +
              "────────────────────\n" +
              current.Sha256;
    }

    /// <summary>
    /// Liefert die verfügbaren Plugins ohne doppelte Datei-/Hashkombinationen.
    /// </summary>
    private List<YacaPluginInfo> GetDistinctPlugins()
    {
        var result = new List<YacaPluginInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in _service.ScanPlugins())
        {
            if (seen.Add($"{plugin.FilePath}|{plugin.Sha256}"))
            {
                result.Add(plugin);
            }
        }

        return result;
    }
}
