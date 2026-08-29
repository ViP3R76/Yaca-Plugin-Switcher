using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
    private const double DashboardVersionListFontSize = 16;
    private const double DashboardFooterFontSize = 18;

    private static readonly Dictionary<string, string> DashboardIconData = new(StringComparer.OrdinalIgnoreCase)
    {
        ["switch"] = "M 12,3 A 9,9 0 1 0 21,12 M 21,12 A 9,9 0 0 0 12,3 M 17,2 L 21,3 L 20,7 M 7,22 L 3,21 L 4,17",
        ["backup"] = "M 4,4 L 20,4 L 20,20 L 4,20 Z M 12,7 L 12,17 M 7,12 L 17,12",
        ["updater"] = "M 6,17 L 18,17 A 5,5 0 0 0 18,8 A 7,7 0 0 0 5,9 A 4,4 0 0 0 6,17 M 12,10 L 12,20 M 8,16 L 12,20 L 16,16",
        ["backups"] = "M 4,5 C 4,2 20,2 20,5 L 20,19 C 20,22 4,22 4,19 Z M 4,5 C 4,8 20,8 20,5 M 4,12 C 4,15 20,15 20,12",
        ["shield"] = "M 12,2 L 20,5 L 20,11 C 20,16 16.8,20 12,22 C 7.2,20 4,16 4,11 L 4,5 Z",
        ["teamspeak"] = "M 12,3 A 9,9 0 0 0 3,12 M 12,3 A 9,9 0 0 1 21,12 M 5,17 L 9,13 M 19,17 L 15,13 M 8,20 L 16,20"
    };

    private Grid RenderDashboard()
    {
        var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new Grid();
        AddStarColumns(top, 3);
        BuildCurrentInstalledPanel(top, 0);
        BuildFreeBrandLogo(top, 1);
        BuildTeamSpeakPanel(top, 2);
        Grid.SetRow(top, 0);
        root.Children.Add(top);

        var actions = new Grid { Margin = new Thickness(0, 8, 0, 8) };
        AddStarColumns(actions, 3);
        AddDashboardTile(actions, 0, "switch", "YACA WECHSELN", IsGerman ? "Version auswählen\nund wechseln" : "Select a version\nand switch", (Brush)FindResource("AccentBrush"), ShowSwitchPage);
        AddDashboardTile(actions, 1, "backup", "BACKUP ERSTELLEN", IsGerman ? "Aktuelle Version\nsichern" : "Save current version", (Brush)FindResource("GoldBrush"), CreateBackupFromDashboard);
        AddDashboardTile(actions, 2, "updater", "YACA UPDATER", IsGerman ? "Neueste DLL prüfen\nund herunterladen" : "Check and download\nlatest DLL", (Brush)FindResource("AccentBrush"), () => ShowError(IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version."));
        Grid.SetRow(actions, 1);
        root.Children.Add(actions);

        var lower = new Grid();
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        BuildLatestBackupPanel(lower, 0);
        BuildAvailableVersionsPanel(lower, 1);
        Grid.SetRow(lower, 2);
        root.Children.Add(lower);
        return root;
    }

    private void BuildCurrentInstalledPanel(Grid host, int column)
    {
        var card = MakeCard((Brush)FindResource("AccentBrush"));
        _currentCard = card;
        var panel = new Grid { Margin = new Thickness(18) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = CreateDashboardHeader("shield", IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED");
        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _currentValue = new TextBlock { Text = "—", FontSize = DashboardVersionFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center };
        center.Children.Add(_currentValue);
        center.Children.Add(new Border
        {
            Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 10, 0, 0), HorizontalAlignment = HorizontalAlignment.Center,
            Child = new TextBlock { Text = IsGerman ? "AKTIV" : "ACTIVE", Foreground = Brushes.Black, FontSize = DashboardBadgeFontSize, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }
        });
        Grid.SetRow(center, 1);
        panel.Children.Add(center);
        _currentDetails = new TextBlock { FontSize = 13, LineHeight = 20, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(18, 0, 18, 12), TextWrapping = TextWrapping.Wrap };
        Grid.SetRow(_currentDetails, 1);
        panel.Children.Add(_currentDetails);
        card.Child = panel;
        Grid.SetColumn(card, column);
        host.Children.Add(card);
    }

    private void BuildFreeBrandLogo(Grid host, int column)
    {
        var logo = new Image { Source = LoadLogo(), Width = 260, Height = 260, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        Grid.SetColumn(logo, column);
        host.Children.Add(logo);
    }

    private void BuildTeamSpeakPanel(Grid host, int column)
    {
        var card = MakeCard((Brush)FindResource("GoldBrush"));
        var panel = new Grid { Margin = new Thickness(18) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader("teamspeak", "TEAMSpeak 3 STATUS");
        Grid.SetRow(header, 0); panel.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _tsStatus = new TextBlock { Text = "—", FontSize = 28, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Foreground = (Brush)FindResource("GoldBrush") };
        _tsDescription = new TextBlock { FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 10, 0, 0), MaxWidth = 360 };
        center.Children.Add(_tsStatus); center.Children.Add(_tsDescription);
        Grid.SetRow(center, 1); panel.Children.Add(center);
        _tsClose = new Button { Content = IsGerman ? "TeamSpeak 3 schließen" : "Close TeamSpeak 3", Visibility = Visibility.Collapsed, Style = (Style)FindResource("TileButtonStyle"), Foreground = (Brush)FindResource("GoldBrush"), BorderBrush = (Brush)FindResource("GoldBrush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
        _tsClose.Click += (_, _) => CloseTeamSpeak(); Grid.SetRow(_tsClose, 2); panel.Children.Add(_tsClose);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildLatestBackupPanel(Grid host, int column)
    {
        var card = MakeCard((Brush)FindResource("BorderBrush")); _backupCard = card;
        var panel = new Grid { Margin = new Thickness(18) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = CreateDashboardHeader("backups", IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP"); Grid.SetRow(header, 0); panel.Children.Add(header);
        _backupSummary = new TextBlock { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Left, Margin = new Thickness(6, 10, 6, 6) }; Grid.SetRow(_backupSummary, 1); panel.Children.Add(_backupSummary);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildAvailableVersionsPanel(Grid host, int column)
    {
        var card = MakeCard((Brush)FindResource("BorderBrush"));
        var panel = new Grid { Margin = new Thickness(18) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader("backups", IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS"); Grid.SetRow(header, 0); panel.Children.Add(header);
        _versionList = new StackPanel { Margin = new Thickness(6, 10, 6, 8) }; Grid.SetRow(_versionList, 1); panel.Children.Add(_versionList);
        var footer = new TextBlock { Text = "0", FontSize = DashboardFooterFontSize, Foreground = (Brush)FindResource("SecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0), Tag = "versions-footer" }; Grid.SetRow(footer, 2); panel.Children.Add(footer);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private StackPanel CreateDashboardHeader(string iconKey, string text)
    {
        var header = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (DashboardIconData.TryGetValue(iconKey, out var data)) header.Children.Add(CreateIcon(data, (Brush)FindResource("AccentBrush"), DashboardHeaderIconSize, DashboardHeaderIconSize, 2.15));
        header.Children.Add(new TextBlock { Text = text, FontSize = DashboardHeaderFontSize, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("AccentBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
        return header;
    }

    private void AddDashboardTile(Grid host, int column, string iconKey, string title, string subtitle, Brush accent, Action action)
    {
        var button = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = accent, Margin = new Thickness(6), Tag = "reference-dashboard-tile" };
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        if (DashboardIconData.TryGetValue(iconKey, out var data)) panel.Children.Add(CreateIcon(data, accent, DashboardTileIconSize, DashboardTileIconSize, 3.6));
        panel.Children.Add(new TextBlock { Text = title, FontSize = DashboardTileTitleFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4) });
        panel.Children.Add(new TextBlock { Text = subtitle, FontSize = DashboardTileSubtitleFontSize, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Center });
        button.Content = panel;
        button.Click += (_, _) => action(); Grid.SetColumn(button, column); host.Children.Add(button);
    }

    private void RenderVersionList(YacaPluginInfo? current)
    {
        if (_versionList is null) return;
        _versionList.Children.Clear();
        var ordered = _plugins.OrderByDescending(p => p.Version).ThenByDescending(p => p.Build).Take(6).ToList();
        var rowHeight = ordered.Count == 0 ? 1 : 1.0 / ordered.Count;
        foreach (var plugin in ordered)
        {
            var row = new Grid { MinHeight = 34 };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock { Text = plugin.Version?.ToString() ?? plugin.DisplayName, FontSize = DashboardVersionListFontSize, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true)
            {
                var badge = new Border { Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2), VerticalAlignment = VerticalAlignment.Center, Child = new TextBlock { Text = IsGerman ? "INSTALLIERT" : "INSTALLED", Foreground = Brushes.Black, FontSize = 10, FontWeight = FontWeights.Bold } };
                Grid.SetColumn(badge, 1); row.Children.Add(badge);
            }
            _versionList.Children.Add(row);
        }
        var footer = FindVisualTextBlocks(PageHost).FirstOrDefault(t => Equals(t.Tag, "versions-footer"));
        if (footer is not null) footer.Text = $"{_plugins.Count.ToString(CultureInfo.InvariantCulture)} {(IsGerman ? "Versionen verfügbar" : "versions available")}";
    }

    private static void AddStarColumns(Grid grid, int count) { for (var i = 0; i < count; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); }
    private static System.Windows.Shapes.Path CreateIcon(string data, Brush stroke, double width, double height, double thickness) => new() { Data = Geometry.Parse(data), Stroke = stroke, StrokeThickness = thickness, Fill = Brushes.Transparent, Width = width, Height = height, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private static BitmapImage LoadLogo() => new(new Uri("pack://application:,,,/YacaPluginSwitcher;component/Assets/yaca_logo.png"));
    private static IEnumerable<TextBlock> FindVisualTextBlocks(DependencyObject root) { foreach (var child in LogicalTreeHelper.GetChildren(root)) { if (child is TextBlock text) yield return text; if (child is DependencyObject dependency) foreach (var nested in FindVisualTextBlocks(dependency)) yield return nested; } }
}
