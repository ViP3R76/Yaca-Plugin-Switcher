using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Erstellt die Navigation aus den zentral registrierten Einträgen.
    /// </summary>
    private void BuildNavigation()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();

        AddNav("home", DashboardIconRegistry.IconAssetDashboard, "Dashboard", () => ShowHome());
        AddNav("refresh", DashboardIconRegistry.IconAssetRefresh, IsGerman ? "Aktualisieren" : "Refresh", () =>
        {
            var status = IsGerman ? "Aktualisierung wird ausgeführt …" : "Refreshing …";
            SetGlobalStatus(status);
            RefreshActivePage(false);
            _ = ClearTemporaryRefreshStatusAsync(status);
        });
        AddNav("switch", DashboardIconRegistry.IconAssetSync, IsGerman ? "YACA wechseln" : "Switch YACA", () => ShowSwitchPage());
        AddNav("updater", DashboardIconRegistry.IconAssetSync, "YACA Updater", () =>
        {
            ShowSwitchPage();
            Dispatcher.BeginInvoke(new Action(() => SetActiveNav("updater")), System.Windows.Threading.DispatcherPriority.Background);
        });

        NavPanel.Children.Add(new Separator
        {
            Margin = new Thickness(10, 12, 0, 12),
            Background = (Brush)FindResource("AccentSoftBrush")
        });

        AddNav("backup-create", DashboardIconRegistry.IconAssetBackup, IsGerman ? "Backup erstellen" : "Create Backup", () => CreateBackupFromDashboard());
        AddNav("backups", DashboardIconRegistry.IconAssetBackups, IsGerman ? "Backup verwalten" : "Manage Backups", () => ShowBackups());

        NavPanel.Children.Add(new Separator
        {
            Margin = new Thickness(10, 12, 0, 12),
            Background = (Brush)FindResource("AccentSoftBrush")
        });

        AddNav("info", DashboardIconRegistry.IconAssetInfo, "Info & Links", () => ShowInfo());

        ExitNavContent.Children.Clear();
        ConfigureNavContent(ExitNavContent, DashboardIconRegistry.IconAssetExit, IsGerman ? "Beenden" : "Exit");
    }

    /// <summary>
    /// Entfernt den temporären Status nach einer erfolgreichen Aktualisierung,
    /// sofern inzwischen kein anderer Status gesetzt wurde.
    /// </summary>
    private async Task ClearTemporaryRefreshStatusAsync(string expectedStatus)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));

        if (string.Equals(GlobalFooterStatusText.Text, expectedStatus, StringComparison.Ordinal))
        {
            SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
        }
    }
}
