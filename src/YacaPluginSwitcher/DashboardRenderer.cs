using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

/// <summary>Single source of truth for the complete dashboard visualization.</summary>
public partial class MainWindow
{
    private const double DashboardPanelHeight = 286;
    private const double DashboardHeaderFontSize = 28;
    private const double DashboardHeaderIconSize = 28;
    private const double DashboardTileIconSize = 92;
    private const double DashboardTileTitleFontSize = 23;
    private const double DashboardTileSubtitleFontSize = 14;
    private const double DashboardVersionFontSize = 38;
    private const double DashboardBadgeFontSize = 14;
    private const double DashboardVersionListFontSize = 17;
    private const double DashboardFooterFontSize = 18;
    private Button? _versionsFooter;

    private static readonly Dictionary<string, string> DashboardIconData = new(StringComparer.OrdinalIgnoreCase)
    {
        ["home"] = "M 3,11 L 12,3 L 21,11 V 21 H 15 V 14 H 9 V 21 H 3 Z",
        ["refresh"] = "M 20,11 A 8,8 0 1 0 20,16 M 20,4 V 11 H 13",
        ["switch"] = "M 19.5,7.5 A 8.5,8.5 0 0 0 5.2,5.2 L 3.4,7 M 3.4,7 V 2.8 M 3.4,7 H 7.6 M 4.5,16.5 A 8.5,8.5 0 0 0 18.8,18.8 L 20.6,17 M 20.6,17 V 21.2 M 20.6,17 H 16.4",
        ["backup"] = "M 4,4 H 20 V 20 H 4 Z M 12,7 V 17 M 7,12 H 17",
        ["updater"] = "M 192,395 H 139 A 96,96 0 0 1 139,203 A 123,123 0 0 1 385,204 A 100,100 0 0 1 385,404 H 331 M 256,214 V 358 M 198,300 L 256,358 L 314,300",
        ["backups"] = "M 4,5 C 4,2 20,2 20,5 V 19 C 20,22 4,22 4,19 Z M 4,5 C 4,8 20,8 20,5 M 4,12 C 4,15 20,15 20,12",
        ["shield"] = "M 12,2 L 20,5 V 11 C 20,16 16.8,20 12,22 C 7.2,20 4,16 4,11 V 5 Z",
        ["info"] = "M 12,21 A 9,9 0 1 0 12,3 A 9,9 0 0 0 12,21 M 12,10 V 16 M 12,7 V 7"
    };

    private const string TeamSpeakLogoData = "M 0.123421 42.6798 C 0.260552 41.4132 -0.0137098 39.9333 0.260552 38.4001 C 0.671945 35.9202 2.11182 33.987 4.38819 32.9204 C 4.93672 32.6537 5.21098 32.3871 5.34811 31.6538 C 6.30803 26.3075 8.58439 21.3611 11.8892 16.9347 C 12.3006 16.4014 12.5749 16.1348 12.0264 15.4015 C 11.4779 14.6015 11.8892 13.7349 12.4378 13.0549 C 17.1139 7.97525 22.6129 4.22882 29.154 2.14894 C 44.8418 -2.66409 58.5961 0.615706 70.4991 11.9083 C 71.5961 12.9749 73.0497 13.9749 71.5961 15.7881 C 71.3218 16.0548 71.7332 16.3214 72.0075 16.5881 C 75.3809 21.1344 77.6436 26.2141 78.6858 31.7071 C 78.8229 32.2404 79.2343 32.5071 79.6457 32.7737 C 82.4706 34.1736 83.9104 36.5202 83.9104 39.6666 C 83.9104 42.8131 84.1847 45.1463 83.7733 47.8928 C 83.0877 51.9059 78.96 54.6524 74.9695 53.7724 C 73.8725 53.5058 73.3925 52.7058 73.3925 51.5592 C 73.3925 47.0129 73.5296 42.4665 73.3925 37.9201 C 72.9811 25.6808 67.4822 16.4547 56.5391 10.5618 C 38.5201 0.935687 15.1255 11.8283 11.2036 31.7605 C 10.5179 35.1736 10.6551 38.7867 10.6551 42.1998 C 10.6551 45.6129 10.6551 48.6928 10.5179 51.9592 C 10.5179 53.0258 9.83228 53.7591 8.52954 53.7591 C 3.37342 54.0258 0 50.8793 0 45.7996 C 0.137131 44.9996 0.137131 43.9997 0.137131 42.6531 M 35.8243 58.7355 C 37.6756 58.0689 39.0606 56.8023 39.472 54.7224 C 39.8834 52.6425 37.4836 49.7761 34.2473 46.7629 C 30.8739 43.6164 26.472 40.27 24.0722 39.07 C 20.5616 37.0035 17.2568 38.8034 16.5711 42.8165 C 15.7483 47.2295 16.5711 51.376 18.9846 55.0557 C 20.6988 57.6689 23.1123 58.8022 26.0743 59.0688 C 27.7199 59.0022 34.3982 59.3355 35.838 58.7355 M 50.9499 60.0021 C 53.0756 60.2687 55.0775 60.6687 57.203 60.802 C 60.0279 60.9354 62.1535 60.1354 63.7442 58.4555 C 65.7326 56.389 66.7062 53.7758 66.5691 51.0293 C 66.4319 48.1495 64.0184 46.4829 60.7959 47.0162 C 57.8338 47.4162 55.2969 48.6828 52.6091 49.6294 C 50.1956 50.5625 48.0701 51.6959 46.356 53.1758 C 43.9425 55.389 45.2589 58.3889 49.0437 59.5355 C 49.5237 59.7354 50.2099 59.8688 50.9636 60.0021 M 73.4531 57.5356 C 73.0417 57.1356 72.356 57.4023 72.0132 57.9356 C 70.7104 61.9486 64.6492 75.3878 44.011 76.8011 C 19.6566 78.4676 57.011 83.5606 67.6798 75.8678 C 71.3275 73.1213 75.4552 70.3881 75.318 62.4953 C 75.318 60.962 74.5638 58.3489 73.4668 57.5489 M 108.875 23.9691 H 113 V 29.6667 H 111.011 C 110.135 29.7066 109.565 29.8663 109.313 30.1725 C 109.061 30.4654 108.915 31.1177 108.889 32.1161 V 63 C 107.231 63 105.467 62.7204 104.393 61.5224 C 103.318 60.3243 103 57.9414 103 56.6368 V 13 H 108.889 V 23.9691 H 108.875 Z";

    private static readonly string LatestBackupFolderIconData = "M 5 6 H 10 L 13 9 H 20 C 21.1 9 22 9.9 22 11 V 18 C 22 19.1 21.1 20 20 20 H 4 C 2.9 20 2 19.1 2 18 V 8 C 2 6.9 2.9 6 4 6 Z M 16.2 12.4 A 4.7 4.7 0 1 0 17.2 17.8 M 16.2 10.2 V 13.7 H 19.7";

    private Grid RenderDashboard()
    {
        var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var top = new Grid(); AddStarColumns(top, 3); BuildCurrentInstalledPanel(top, 0); BuildFreeBrandLogo(top, 1); BuildTeamSpeakPanel(top, 2); Grid.SetRow(top, 0); root.Children.Add(top);
        var actions = new Grid(); AddStarColumns(actions, 3);
        AddDashboardTile(actions, 0, "switch", IsGerman ? "YACA WECHSELN" : "SWITCH YACA", IsGerman ? "Version auswählen\nund wechseln" : "Select a version\nand switch", (Brush)FindResource("AccentBrush"), () => ShowSwitchPage());
        AddDashboardTile(actions, 1, "backup", IsGerman ? "BACKUP ERSTELLEN" : "CREATE BACKUP", IsGerman ? "Aktuelle Version sichern" : "Save current version", (Brush)FindResource("GoldBrush"), CreateBackupFromDashboard);
        AddDashboardTile(actions, 2, "updater", "YACA UPDATER", IsGerman ? "Neueste DLL prüfen\nund herunterladen" : "Check and download\nlatest DLL", (Brush)FindResource("AccentBrush"), ShowComingSoon); Grid.SetRow(actions, 1); root.Children.Add(actions);
        var lower = new Grid(); AddStarColumns(lower, 2); BuildLatestBackupPanel(lower, 0); BuildAvailableVersionsPanel(lower, 1); Grid.SetRow(lower, 2); root.Children.Add(lower); return root;
    }

    private Border CreatePanelCard(Brush borderBrush) => new() { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = borderBrush, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(6) };

    private void BuildCurrentInstalledPanel(Grid host, int column)
    {
        var gold = (Brush)FindResource("GoldBrush"); var card = CreatePanelCard(gold); _currentCard = card;
        var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader("shield", IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", gold); Grid.SetRow(header, 0); panel.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; _currentValue = new TextBlock { Text = "—", FontSize = DashboardVersionFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Foreground = gold }; center.Children.Add(_currentValue);
        center.Children.Add(new Border { Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(16, 5, 16, 5), Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, Child = new TextBlock { Text = IsGerman ? "AKTIV" : "ACTIVE", Foreground = Brushes.Black, FontSize = DashboardBadgeFontSize, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center } }); Grid.SetRow(center, 1); panel.Children.Add(center);
        _currentDetails = new TextBlock { FontSize = 13, LineHeight = 20, Foreground = gold, TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.NoWrap }; Grid.SetRow(_currentDetails, 2); panel.Children.Add(_currentDetails); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private static void BuildFreeBrandLogo(Grid host, int column) { var logo = new Image { Source = LoadLogo(), Width = 260, Height = 260, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false }; Grid.SetColumn(logo, column); host.Children.Add(logo); }

    private void BuildTeamSpeakPanel(Grid host, int column)
    {
        var gold = (Brush)FindResource("GoldBrush"); var card = CreatePanelCard(gold); var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader("teamspeak", "STATUS", gold); Grid.SetRow(header, 0); panel.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; _tsStatus = new TextBlock { Text = "—", FontSize = 28, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = gold }; _tsDescription = new TextBlock { FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0), MaxWidth = 360 }; center.Children.Add(_tsStatus); center.Children.Add(_tsDescription); Grid.SetRow(center, 1); panel.Children.Add(center);
        _tsClose = new Button { Content = IsGerman ? "TeamSpeak 3 schließen" : "Close TeamSpeak 3", Visibility = Visibility.Collapsed, Style = (Style)FindResource("TileButtonStyle"), Foreground = gold, BorderBrush = gold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) }; _tsClose.Click += (_, _) => CloseTeamSpeak(); Grid.SetRow(_tsClose, 2); panel.Children.Add(_tsClose); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildLatestBackupPanel(Grid host, int column)
    {
        var card = CreatePanelCard((Brush)FindResource("BorderBrush")); _backupCard = card;
        var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = CreateDashboardHeader("backups", IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP"); Grid.SetRow(header, 0); panel.Children.Add(header);
        var content = new Grid { Margin = new Thickness(6, 2, 6, 0) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(148) });
        _backupSummary = new TextBlock { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 10, 0) };
        Grid.SetColumn(_backupSummary, 0); content.Children.Add(_backupSummary);
        var folderIcon = new System.Windows.Shapes.Path { Data = Geometry.Parse(LatestBackupFolderIconData), Stroke = (Brush)FindResource("AccentBrush"), StrokeThickness = 2.8, Fill = Brushes.Transparent, Width = 132, Height = 132, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, Opacity = 0.96 };
        Grid.SetColumn(folderIcon, 1); content.Children.Add(folderIcon);
        Grid.SetRow(content, 1); panel.Children.Add(content); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildAvailableVersionsPanel(Grid host, int column)
    {
        var card = CreatePanelCard((Brush)FindResource("BorderBrush")); var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); var header = CreateDashboardHeader("backups", IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS"); Grid.SetRow(header, 0); panel.Children.Add(header);
        _versionList = new StackPanel { Margin = new Thickness(6, 10, 6, 8) }; Grid.SetRow(_versionList, 1); panel.Children.Add(_versionList); _versionsFooter = new Button { Content = "0", FontSize = DashboardFooterFontSize, Foreground = (Brush)FindResource("AccentBrush"), Background = Brushes.Transparent, BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(0), Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(8, 4, 8, 4), HorizontalAlignment = HorizontalAlignment.Center, Tag = "versions-footer" }; _versionsFooter.Click += (_, _) => ShowSwitchPage(); Grid.SetRow(_versionsFooter, 2); panel.Children.Add(_versionsFooter); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private Grid CreateDashboardHeader(string iconKey, string text, Brush? headerBrush = null)
    {
        var brush = headerBrush ?? (Brush)FindResource("AccentBrush");
        var header = new Grid { VerticalAlignment = VerticalAlignment.Center };
        header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });

        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (iconKey.Equals("teamspeak", StringComparison.OrdinalIgnoreCase))
            content.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(TeamSpeakLogoData), Fill = brush, Stroke = null, Width = 104, Height = DashboardHeaderIconSize, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center });
        else if (DashboardIconData.TryGetValue(iconKey, out var data))
            content.Children.Add(CreateIcon(data, brush, DashboardHeaderIconSize, DashboardHeaderIconSize, iconKey.Equals("switch", StringComparison.OrdinalIgnoreCase) ? 3.0 : 2.15));

        content.Children.Add(new TextBlock { Text = text, FontSize = DashboardHeaderFontSize, FontWeight = FontWeights.SemiBold, Foreground = brush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.NoWrap });
        Grid.SetRow(content, 0);
        header.Children.Add(content);

        header.Children.Add(new Border
        {
            Grid.Row = 1,
            Height = 1,
            Background = brush,
            Opacity = 0.65,
            Margin = new Thickness(0, 7, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        });

        return header;
    }

    private void AddDashboardTile(Grid host, int column, string iconKey, string title, string subtitle, Brush accent, Action action)
    {
        var button = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = accent, Margin = new Thickness(6), Tag = "reference-dashboard-tile" }; var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; if (DashboardIconData.TryGetValue(iconKey, out var data)) panel.Children.Add(CreateIcon(data, accent, DashboardTileIconSize, DashboardTileIconSize, iconKey.Equals("switch", StringComparison.OrdinalIgnoreCase) ? 6.0 : 3.6)); panel.Children.Add(new TextBlock { Text = title, FontSize = DashboardTileTitleFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4), TextWrapping = TextWrapping.NoWrap }); panel.Children.Add(new TextBlock { Text = subtitle, FontSize = DashboardTileSubtitleFontSize, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.NoWrap }); button.Content = panel; button.Click += (_, _) => action(); Grid.SetColumn(button, column); host.Children.Add(button);
    }

    private SolidColorBrush GetVersionRowBackground(int index)
    {
        if (index % 2 == 0) return Brushes.Transparent;
        if (FindResource("SurfaceBrush") is SolidColorBrush surface) return new SolidColorBrush(Color.FromArgb(24, surface.Color.R, surface.Color.G, surface.Color.B));
        return Brushes.Transparent;
    }

    private void RenderVersionList(YacaPluginInfo? current)
    {
        if (_versionList is null) return; _versionList.Children.Clear(); var ordered = _plugins.OrderByDescending(p => p.Version).ThenByDescending(p => p.Build).ToList();
        for (var index = 0; index < ordered.Count; index++)
        {
            var plugin = ordered[index]; var row = new Border { Background = GetVersionRowBackground(index), CornerRadius = new CornerRadius(3), Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 1, 0, 1) };
            var grid = new Grid { MinHeight = 34 }; grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new TextBlock { Text = $"YACA {plugin.Version} - (Build: {plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"})", FontSize = DashboardVersionListFontSize, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true) { var badge = new Border { Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Child = new TextBlock { Text = IsGerman ? "INSTALLIERT" : "INSTALLED", Foreground = Brushes.Black, FontSize = 10, FontWeight = FontWeights.Bold } }; Grid.SetColumn(badge, 1); grid.Children.Add(badge); }
            row.Child = grid; _versionList.Children.Add(row);
        }
        if (_versionsFooter is not null) _versionsFooter.Content = $"{_plugins.Count.ToString(CultureInfo.InvariantCulture)} {(IsGerman ? "Versionen verfügbar – YACA wechseln" : "versions available – switch YACA")}";
    }

    private void UpdateBackupSummary(BackupInfo? backup)
    {
        if (_backupSummary is null) return; _backupSummary.Inlines.Clear(); if (backup is null) { _backupSummary.Inlines.Add(new Run(Texts.NoBackups)); return; }
        _backupSummary.Inlines.Add(new Run($"{backup.Timestamp:dd.MM.yyyy HH:mm}") { FontSize = 34, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("GoldBrush") }); _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"{backup.DisplayName}  •  {(backup.IsAutomatic ? (IsGerman ? "Automatisch" : "Automatic") : (IsGerman ? "Manuell" : "Manual"))}") { FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("ForegroundBrush") }); _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"{(IsGerman ? "Datei" : "File")}    {backup.FileName}") { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush") }); _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"{(IsGerman ? "Größe" : "Size")}    {backup.FileSize / 1024d / 1024d:0.00} MB") { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush") });
    }

    private static void AddStarColumns(Grid grid, int count) { for (var i = 0; i < count; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); }
    private static System.Windows.Shapes.Path CreateIcon(string data, Brush stroke, double width, double height, double thickness) => new() { Data = Geometry.Parse(data), Stroke = stroke, StrokeThickness = thickness, Fill = Brushes.Transparent, Width = width, Height = height, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private static BitmapImage LoadLogo() => new(new Uri("pack://application:,,,/YacaPluginSwitcher;component/Assets/yaca_logo.png"));
    private static IEnumerable<TextBlock> FindVisualTextBlocks(DependencyObject root) { foreach (var child in LogicalTreeHelper.GetChildren(root)) { if (child is TextBlock text) yield return text; if (child is DependencyObject dependency) foreach (var nested in FindVisualTextBlocks(dependency)) yield return nested; } }
}
