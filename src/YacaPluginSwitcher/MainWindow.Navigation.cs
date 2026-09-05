using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
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
        AddNav("updater", DashboardIconRegistry.IconAssetSync, "YACA Updater", () => ShowUpdaterPage());

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

        AddNav("config", DashboardIconRegistry.IconAssetSettings, IsGerman ? "Einstellungen" : "Settings", () => ShowConfig());
        AddNav("info", DashboardIconRegistry.IconAssetInfo, "Info & Links", () => ShowInfo());

        ExitNavContent.Children.Clear();
        if (ExitNavContent.Parent is StackPanel exitHost && exitHost.Children.OfType<Button>().FirstOrDefault() is { } exitButton)
            ConfigureNavContent(ExitNavContent, exitButton, DashboardIconRegistry.IconAssetExit, IsGerman ? "Beenden" : "Exit");
    }

    private async Task ClearTemporaryRefreshStatusAsync(string expectedStatus)
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        if (string.Equals(GlobalFooterStatusText.Text, expectedStatus, StringComparison.Ordinal))
            SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
    }
}
