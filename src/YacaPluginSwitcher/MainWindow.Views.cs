using System.Windows;
using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Öffnet die Konfigurationsansicht.
    /// </summary>
    private void ShowConfig()
    {
        _activePage = "config";
        SetActiveNav("config");
        PageHost.Content = new ConfigView(_service, this);
        SetGlobalStatus(
            IsGerman
                ? "Konfiguration geöffnet."
                : "Configuration opened.");
    }

    /// <summary>
    /// Öffnet die Informationsansicht.
    /// </summary>
    private void ShowInfo()
    {
        _activePage = "info";
        SetActiveNav("info");
        PageHost.Content = new InfoView(_service.Settings.Language);
        SetGlobalStatus(
            IsGerman
                ? "Info & Links geöffnet."
                : "Info & Links opened.");
    }

    /// <summary>
    /// Springt aus untergeordneten Ansichten zurück zum Dashboard.
    /// </summary>
    internal void ReturnHome()
    {
        ShowHome();
    }

    /// <summary>
    /// Aktualisiert die aktuell sichtbare Ansicht.
    /// </summary>
    private void RefreshActivePage(bool announce)
    {
        switch (_activePage)
        {
            case "home":
                RefreshHome(announce);
                break;

            case "switch":
                ShowSwitchPage();
                break;

            case "backups":
                PageHost.Content = new BackupView(_service, this);
                break;

            case "config":
                PageHost.Content = new ConfigView(_service, this);
                break;

            case "info":
                ShowInfo();
                break;
        }
    }

    /// <summary>
    /// Bewegt das Fenster per linker Maustaste über die eigene Titelleiste.
    /// Ein Doppelklick schaltet zwischen maximiertem und normalem Zustand um.
    /// </summary>
    private void TitleBar_MouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    /// <summary>
    /// Minimiert das Hauptfenster.
    /// </summary>
    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Wechselt zwischen normalem und maximiertem Fensterzustand.
    /// </summary>
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    /// <summary>
    /// Beendet die Anwendung und bricht einen laufenden Updater ab.
    /// </summary>
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _updaterCts?.Cancel();
        Close();
    }
}
