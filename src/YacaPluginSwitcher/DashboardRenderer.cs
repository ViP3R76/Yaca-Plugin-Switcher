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

    // TeamSpeak headset mark. The official inline logo also contains the TEAMSPEAK wordmark;
    // the wordmark is rendered beside this vector so the complete branding remains visible at header scale.
    private const string TeamSpeakLogoData = "M 3 12 C 3 7 7 3 12 3 C 17 3 21 7 21 12 V 17 C 21 19 19 20 17 20 H 16 V 13 H 19 V 17 M 3 17 C 3 19 5 20 7 20 H 8 V 13 H 5 V 17";

    // Folder + circular restore/sync mark, sized and proportioned for the reference backup panel.
    private static readonly string LatestBackupFolderIconData = "M 5 5 H 11 L 14 8 H 22 C 23.1 8 24 8.9 24 10 V 19 C 24 20.1 23.1 21 22 21 H 4 C 2.9 21 2 20.1 2 19 V 7 C 2 5.9 2.9 5 4 5 Z M 18.5 13.5 A 5.5 5.5 0 1 0 19.5 19 M 18.5 11.5 V 15 H 22";

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
        _currentDetails = new TextBlock { FontSize = 13, LineHeight = 20, Foreground = (Brush)FindResource("ForegroundBrush"), TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.NoWrap }; Grid.SetRow(_currentDetails, 2); panel.Children.Add(_currentDetails); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
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
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        _backupSummary = new TextBlock { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 10, 0) };
        Grid.SetColumn(_backupSummary, 0); content.Children.Add(_backupSummary);
        var folderIcon = new System.Windows.Shapes.Path { Data = Geometry.Parse(LatestBackupFolderIconData), Stroke = (Brush)FindResource("AccentBrush"), StrokeThickness = 3.1, Fill = Brushes.Transparent, Width = 142, Height = 142, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, Opacity = 0.98 };
        Grid.SetColumn(folderIcon, 1); content.Children.Add(folderIcon);
        Grid.SetRow(content, 1); panel.Children.Add(content); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildAvailableVersionsPanel(Grid host, int column)
    {
        var card = CreatePanelCard((Brush)FindResource("BorderBrush")); var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); var header = CreateDashboardHeader("backups", IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS"); Grid.SetRow(header, 0); panel.Children.Add(header);
        _versionList = new StackPanel { Margin = new Thickness(6, 10, 6, 8) }; Grid.SetRow(_versionList, 1); panel.Children.Add(_versionList);
        var footer = new TextBlock { Content = null, Text = "0", FontSize = DashboardFooterFontSize, Foreground = (Brush)FindResource("AccentBrush"), Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Cursor = System.Windows.Input.Cursors.Hand, Padding = new Thickness(8, 4, 8, 4), Tag = "versions-footer" };
        footer.MouseLeftButtonUp += (_, _) => ShowSwitchPage();
        _versionsFooter = null;
        Grid.SetRow(footer, 2); panel.Children.Add(footer); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
        _versionsFooterText = footer;
    }

    private TextBlock? _versionsFooterText;

    private Grid CreateDashboardHeader(string iconKey, string text, Brush? headerBrush = null)
    {
        var brush = headerBrush ?? (Brush)FindResource("AccentBrush");
        var header = new Grid { VerticalAlignment = VerticalAlignment.Center };
        header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (iconKey.Equals("teamspeak", StringComparison.OrdinalIgnoreCase))
        {
            var tsGroup = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            tsGroup.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse(TeamSpeakLogoData), Stroke = brush, StrokeThickness = 2.1, Fill = Brushes.Transparent, Width = DashboardHeaderIconSize, Height = DashboardHeaderIconSize, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center });
            tsGroup.Children.Add(new TextBlock { Text = "TEAMSPEAK", FontSize = 9.5, FontWeight = FontWeights.Bold, Foreground = brush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 0, 0), TextWrapping = TextWrapping.NoWrap });
            content.Children.Add(tsGroup);
        }
        else if (DashboardIconData.TryGetValue(iconKey, out var data))
            content.Children.Add(CreateIcon(data, brush, DashboardHeaderIconSize, DashboardHeaderIconSize, iconKey.Equals("switch", StringComparison.OrdinalIgnoreCase) ? 3.0 : 2.15));

        content.Children.Add(new TextBlock { Text = text, FontSize = DashboardHeaderFontSize, FontWeight = FontWeights.SemiBold, Foreground = brush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), TextWrapping = TextWrapping.NoWrap });
        Grid.SetRow(content, 0);
        header.Children.Add(content);

        var separator = new Border { Height = 1, Background = brush, Opacity = 0.65, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetRow(separator, 1);
        header.Children.Add(separator);
        return header;
    }

    private void AddDashboardTile(Grid host, int column, string iconKey, string title, string subtitle, Brush accent, Action action)
    {
        var button = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = accent, Margin = new Thickness(6), Tag = "reference-dashboard-tile" }; var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; if (DashboardIconData.TryGetValue(iconKey, out var data)) panel.Children.Add(CreateIcon(data, accent, DashboardTileIconSize, DashboardTileIconSize, iconKey.Equals("switch", StringComparison.OrdinalIgnoreCase) ? 6.0 : 3.6)); panel.Children.Add(new TextBlock { Text = title, FontSize = DashboardTileTitleFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4), TextWrapping = TextWrapping.NoWrap }); panel.Children.Add(new TextBlock { Text = subtitle, FontSize = DashboardTileSubtitleFontSize, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.NoWrap }); button.Content = panel; button.Click += (_, _) => action(); Grid.SetColumn(button, column); host.Children.Add(button);
    }

    private SolidColorBrush GetVersionRowBackground(int index)
    {
        if (index % 2 == 0) return Brushes.Transparent;
        if (FindResource("AccentBrush") is SolidColorBrush accent) return new SolidColorBrush(Color.FromArgb(18, accent.Color.R, accent.Color.G, accent.Color.B));
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
        if (_versionsFooterText is not null) _versionsFooterText.Text = $"{_plugins.Count.ToString(CultureInfo.InvariantCulture)} {(IsGerman ? "Versionen verfügbar – YACA wechseln" : "versions available – switch YACA")}";
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
