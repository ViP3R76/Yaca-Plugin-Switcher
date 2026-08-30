using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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

        AddNav("home", DashboardIconRegistry.IconAssetDashboard, "Dashboard", ShowHome);
        AddNav("refresh", DashboardIconRegistry.IconAssetRefresh, IsGerman ? "Aktualisieren" : "Refresh", () =>
        {
            SetGlobalStatus(IsGerman ? "Aktualisierung wird ausgeführt …" : "Refreshing …");
            RefreshActivePage(false);
        });
        AddNav("switch", DashboardIconRegistry.IconAssetSync, IsGerman ? "YACA wechseln" : "Switch YACA", ShowSwitchPage);
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

        AddNav("backup-create", DashboardIconRegistry.IconAssetBackup, IsGerman ? "Backup erstellen" : "Create Backup", CreateBackupFromDashboard);
        AddNav("backups", DashboardIconRegistry.IconAssetBackups, IsGerman ? "Backup verwalten" : "Manage Backups", ShowBackups);

        NavPanel.Children.Add(new Separator
        {
            Margin = new Thickness(10, 12, 0, 12),
            Background = (Brush)FindResource("AccentSoftBrush")
        });

        AddNav("info", DashboardIconRegistry.IconAssetInfo, "Info & Links", ShowInfo);

        ExitNavContent.Children.Clear();
        ConfigureNavContent(ExitNavContent, DashboardIconRegistry.IconAssetExit, IsGerman ? "Beenden" : "Exit");
    }

    /// <summary>
    /// Erstellt den Inhalt eines Navigationsbuttons.
    /// Die SVG-Füllfarbe ist direkt an die Foreground-Farbe des Buttons gebunden.
    /// Dadurch übernimmt das zentrale Navigation-MouseOver automatisch auch das Icon.
    /// </summary>
    private static void ConfigureNavContent(StackPanel content, string iconAssetKey, string text, Brush? iconBrush = null)
    {
        content.Orientation = Orientation.Horizontal;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Children.Clear();

        var foregroundBrush = iconBrush
            ?? Application.Current.FindResource("ForegroundBrush") as Brush
            ?? Brushes.White;

        var icon = DashboardIconRegistry.CreateIcon(iconAssetKey, foregroundBrush, 30, 30);
        icon.SetBinding(System.Windows.Shapes.Shape.FillProperty, new Binding("Foreground")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });

        content.Children.Add(icon);
        content.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        });
    }

    /// <summary>
    /// Fügt einen Eintrag zur linken Navigation hinzu.
    /// </summary>
    private void AddNav(string key, string iconAssetKey, string text, Action action)
    {
        var content = new StackPanel();
        ConfigureNavContent(content, iconAssetKey, text);

        var button = new Button
        {
            Style = (Style)FindResource("NavButtonStyle"),
            Height = 46,
            Tag = key,
            Content = content
        };

        button.Click += (_, _) => action();
        NavPanel.Children.Add(button);
        _navButtons.Add((key, button));
    }

    /// <summary>
    /// Aktualisiert den visuellen Zustand aller Navigationseinträge.
    /// </summary>
    private void SetActiveNav(string key)
    {
        _activePage = key;

        foreach (var item in _navButtons)
        {
            var selected = item.Key.Equals(key, StringComparison.OrdinalIgnoreCase);

            item.Button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
            item.Button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
            item.Button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
            item.Button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);
        }
    }

    /// <summary>
    /// Zeigt die aktuell ausgewählte Seite nach einem Sprachwechsel erneut an.
    /// </summary>
    private void ShowCurrentPageAfterLanguageChange()
    {
        switch (_activePage)
        {
            case "switch": ShowSwitchPage(); break;
            case "backups": ShowBackups(); break;
            case "config": ShowConfig(); break;
            case "info": ShowInfo(); break;
            default: ShowHome(); break;
        }
    }
}
