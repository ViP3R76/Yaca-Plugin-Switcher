using System.Windows;
using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void ShowConfig()
    {
        _activePage = "config";
        SetActiveNav("config");
        PageHost.Content = new ConfigView(_service, this);
        SetGlobalStatus(IsGerman ? "Konfiguration geöffnet." : "Configuration opened.");
    }

    private void ShowInfo()
    {
        _activePage = "info";
        SetActiveNav("info");
        PageHost.Content = new InfoView(_service.Settings.Language);
        SetGlobalStatus(IsGerman ? "Info & Links geöffnet." : "Info & Links opened.");
    }

    internal void ReturnHome() => ShowHome();

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
            case "updater":
                ShowUpdaterPage();
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        _updaterCts?.Cancel();
        Close();
    }
}
