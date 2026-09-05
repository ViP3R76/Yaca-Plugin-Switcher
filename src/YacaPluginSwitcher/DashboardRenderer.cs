using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private const double DashboardPanelHeight = 286;
    private const double DashboardHeaderFontSize = 28;
    private const double DashboardHeaderIconSize = 28;
    private const double DashboardTileIconSize = 92;
    private const double DashboardTileTitleFontSize = 28;
    private const double DashboardTileSubtitleFontSize = 14;
    private const double DashboardVersionFontSize = 38;
    private const double DashboardBadgeFontSize = 14;
    private const double DashboardVersionListFontSize = 17;
    private const double DashboardFooterFontSize = 18;

    private TextBlock? _versionsFooterText;
    private Grid? _currentDetailsPanel;
    private TextBlock? _currentMetaText;
    private TextBlock? _currentShaLabel;
    private TextBlock? _currentShaValue;
    private Image? _teamSpeakStatusIcon;

    private Grid RenderDashboard()
    {
        var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(DashboardPanelHeight) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new Grid();
        AddStarColumns(top, 3);
        BuildCurrentInstalledPanel(top, 0);
        BuildTeamSpeakPanel(top, 2);
        Grid.SetRow(top, 0);
        root.Children.Add(top);

        var actions = new Grid();
        AddStarColumns(actions, 3);
        AddDashboardTile(actions, 0, DashboardIconRegistry.IconAssetSync,
            IsGerman ? "YACA WECHSELN" : "SWITCH YACA",
            IsGerman ? "Version auswählen\nund wechseln" : "Select a version\nand switch",
            (Brush)FindResource("AccentBrush"), () => ShowSwitchPage());
        AddDashboardTile(actions, 1, DashboardIconRegistry.IconAssetBackup,
            IsGerman ? "BACKUP ERSTELLEN" : "CREATE BACKUP",
            IsGerman ? "Aktuelle Version sichern" : "Save current version",
            (Brush)FindResource("GoldBrush"), CreateBackupFromDashboard);
        AddDashboardTile(actions, 2, DashboardIconRegistry.IconAssetUpdater,
            "YACA UPDATER",
            IsGerman ? "Neueste DLL prüfen\nund herunterladen" : "Check and download\nlatest DLL",
            (Brush)FindResource("AccentBrush"), () => ShowSwitchPage());
        Grid.SetRow(actions, 1);
        root.Children.Add(actions);

        var lower = new Grid();
        AddStarColumns(lower, 2);
        BuildLatestBackupPanel(lower, 0);
        BuildAvailableVersionsPanel(lower, 1);
        Grid.SetRow(lower, 2);
        root.Children.Add(lower);
        return root;
    }

    private Border CreatePanelCard(Brush borderBrush)
    {
        return new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = borderBrush,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(0),
            Padding = new Thickness(20), Margin = new Thickness(6)
        };
    }

    private void BuildCurrentInstalledPanel(Grid host, int column)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCard(gold);
        _currentCard = card;
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetInstalled, IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", gold);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        _currentValue = new TextBlock { Text = "—", FontSize = DashboardVersionFontSize, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center, Foreground = (Brush)FindResource("ForegroundBrush") };
        center.Children.Add(_currentValue);
        var activeBadge = new Border
        {
            Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(0), MinHeight = 30,
            Padding = new Thickness(16, 4, 16, 4), Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock { Text = IsGerman ? "AKTIV" : "ACTIVE", Foreground = Brushes.Black, FontSize = DashboardBadgeFontSize, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        center.Children.Add(activeBadge);
        Grid.SetRow(center, 1);
        panel.Children.Add(center);
        _currentDetails = new DashboardDetailsTextBlock(UpdateCurrentInstalledDetailsFromText) { Visibility = Visibility.Collapsed };
        _currentDetailsPanel = new Grid { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 8, 0, 0) };
        AddAutoRow(_currentDetailsPanel, 4);
        _currentMetaText = new TextBlock { FontSize = 13, LineHeight = 20, Foreground = (Brush)FindResource("ForegroundBrush"), TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, TextWrapping = TextWrapping.NoWrap };
        _currentShaLabel = new TextBlock { FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("ForegroundBrush"), TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 3) };
        var shaSeparator = new Border { Height = 1, Background = (Brush)FindResource("ForegroundBrush"), Opacity = 0.65, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 0, 5) };
        _currentShaValue = new TextBlock { FontSize = 11, Foreground = (Brush)FindResource("ForegroundBrush"), TextAlignment = TextAlignment.Left, HorizontalAlignment = HorizontalAlignment.Left, TextWrapping = TextWrapping.NoWrap };
        Grid.SetRow(_currentMetaText, 0); Grid.SetRow(_currentShaLabel, 1); Grid.SetRow(shaSeparator, 2); Grid.SetRow(_currentShaValue, 3);
        _currentDetailsPanel.Children.Add(_currentMetaText); _currentDetailsPanel.Children.Add(_currentShaLabel); _currentDetailsPanel.Children.Add(shaSeparator); _currentDetailsPanel.Children.Add(_currentShaValue);
        Grid.SetRow(_currentDetailsPanel, 2);
        panel.Children.Add(_currentDetailsPanel);
        card.Child = panel;
        Grid.SetColumn(card, column);
        host.Children.Add(card);
    }

    private void UpdateCurrentInstalledDetailsFromText(string text)
    {
        if (_currentDetailsPanel is null || _currentMetaText is null || _currentShaLabel is null || _currentShaValue is null)
            return;
        var lines = text.Split('\n', StringSplitOptions.None);
        if (lines.Length < 5 || string.IsNullOrWhiteSpace(lines[0]))
        {
            _currentDetailsPanel.Visibility = Visibility.Collapsed;
            return;
        }
        _currentDetailsPanel.Visibility = Visibility.Visible;
        _currentMetaText.Text = string.Join(Environment.NewLine, lines.Take(2));
        _currentShaLabel.Text = lines[2].Trim();
        _currentShaValue.Text = lines[4].Trim();
    }

    private void BuildTeamSpeakPanel(Grid host, int column)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCard(gold);
        var panel = new Grid();
        AddAutoRow(panel);
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        AddAutoRow(panel);
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetTeamSpeakStatus, "TEAMSPEAK STATUS", gold);
        Grid.SetRow(header, 0); panel.Children.Add(header);
        var content = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0) };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _teamSpeakStatusIcon = DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetTeamSpeakStopped, (Brush)FindResource("GoldBrush"), 44, 44);
        _teamSpeakStatusIcon.HorizontalAlignment = HorizontalAlignment.Left;
        _teamSpeakStatusIcon.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(_teamSpeakStatusIcon, 0); content.Children.Add(_teamSpeakStatusIcon);
        var textPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left };
        _tsStatus = new TextBlock { Text = "—", FontSize = 28, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Left, Foreground = gold };
        _tsDescription = new TextBlock { FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.NoWrap, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 10, 0, 0), MaxWidth = 420 };
        textPanel.Children.Add(_tsStatus); textPanel.Children.Add(_tsDescription);
        Grid.SetColumn(textPanel, 1); content.Children.Add(textPanel);
        Grid.SetRow(content, 1); panel.Children.Add(content);
        _tsClose = new Button
        {
            Content = IsGerman ? "TeamSpeak 3 schließen" : "Close TeamSpeak 3", Visibility = Visibility.Collapsed,
            Background = (Brush)FindResource("ErrorBrush"), Foreground = Brushes.White, BorderBrush = (Brush)FindResource("ErrorBrush"), BorderThickness = new Thickness(0),
            Padding = new Thickness(18, 8, 18, 8), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0), Cursor = System.Windows.Input.Cursors.Hand,
            FontSize = 17, FontWeight = FontWeights.Bold
        };
        _tsClose.Template = CreateSquareButtonTemplate();
        _tsClose.Click += (_, _) => CloseTeamSpeak();
        Grid.SetRow(_tsClose, 2); panel.Children.Add(_tsClose);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void BuildAvailableVersionsPanel(Grid host, int column)
    {
        var purple = (Brush)FindResource("AccentBrush");
        var card = CreatePanelCard(purple);
        var panel = new Grid();
        AddAutoRow(panel); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); AddAutoRow(panel);
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetBackups, IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS", purple);
        Grid.SetRow(header, 0); panel.Children.Add(header);
        _versionList = new StackPanel { Margin = new Thickness(6, 10, 6, 8) }; Grid.SetRow(_versionList, 1); panel.Children.Add(_versionList);
        var footer = new Grid { Margin = new Thickness(6, 0, 6, 2), Cursor = System.Windows.Input.Cursors.Hand, Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Stretch };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var footerText = new TextBlock { Text = "0", FontSize = DashboardFooterFontSize, Foreground = purple, Background = Brushes.Transparent, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(8, 4, 8, 4), IsHitTestVisible = false };
        Grid.SetColumn(footerText, 0); footer.Children.Add(footerText);
        var footerArrow = new System.Windows.Shapes.Path { Data = Geometry.Parse("M 2 2 L 8 8 L 2 14"), Stroke = purple, StrokeThickness = 2.2, Fill = Brushes.Transparent, Width = 18, Height = 18, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 8, 0), IsHitTestVisible = false };
        Grid.SetColumn(footerArrow, 1); footer.Children.Add(footerArrow); footer.MouseLeftButtonUp += (_, _) => ShowSwitchPage(); Grid.SetRow(footer, 2); panel.Children.Add(footer);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card); _versionsFooterText = footerText;
    }

    private Grid CreateDashboardHeader(string iconAssetKey, string text, Brush? headerBrush = null)
    {
        var brush = headerBrush ?? (Brush)FindResource("AccentBrush");
        var header = new Grid { VerticalAlignment = VerticalAlignment.Center };
        AddAutoRow(header); AddAutoRow(header);
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(DashboardIconRegistry.CreateIcon(iconAssetKey, brush, DashboardHeaderIconSize, DashboardHeaderIconSize));
        content.Children.Add(new TextBlock { Text = text, FontSize = DashboardHeaderFontSize, FontWeight = FontWeights.SemiBold, Foreground = brush, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) });
        Grid.SetRow(content, 0); header.Children.Add(content);
        var separator = new Border { Height = 1, Background = brush, Opacity = 0.65, Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
        Grid.SetRow(separator, 1); header.Children.Add(separator);
        return header;
    }

    private void AddDashboardTile(Grid host, int column, string iconAssetKey, string title, string subtitle, Brush accent, Action action)
    {
        var surface = (Brush)FindResource("SurfaceBrush");
        var button = new Button { Style = (Style)FindResource("TileButtonStyle"), Background = surface, Foreground = accent, BorderBrush = accent, BorderThickness = new Thickness(1), Margin = new Thickness(6) };
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(DashboardIconRegistry.CreateIcon(iconAssetKey, accent, DashboardTileIconSize, DashboardTileIconSize));
        panel.Children.Add(new TextBlock { Text = title, FontSize = DashboardTileTitleFontSize, FontWeight = FontWeights.Bold, Foreground = accent, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 4) });
        panel.Children.Add(new TextBlock { Text = subtitle, FontSize = DashboardTileSubtitleFontSize, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.NoWrap });
        button.Content = panel; button.Click += (_, _) => action(); Grid.SetColumn(button, column); host.Children.Add(button);
    }

    private void BuildLatestBackupPanel(Grid host, int column)
    {
        var purple = (Brush)FindResource("AccentBrush");
        var card = CreatePanelCard(purple); _backupCard = card;
        var panel = new Grid(); AddAutoRow(panel); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetBackup, IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP"); Grid.SetRow(header, 0); panel.Children.Add(header);
        var content = new Grid { Margin = new Thickness(6, 16, 6, 0), VerticalAlignment = VerticalAlignment.Center };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        _backupSummary = new TextBlock { FontSize = 15, LineHeight = 22, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left };
        Grid.SetColumn(_backupSummary, 0); content.Children.Add(_backupSummary);
        var button = new Button { Content = IsGerman ? "BACKUP VERWALTEN" : "MANAGE BACKUPS", Style = (Style)FindResource("NormalActionButtonStyle"), Margin = new Thickness(12, 0, 0, 0), Height = 42, VerticalAlignment = VerticalAlignment.Center };
        button.Click += (_, _) => ShowBackups(); Grid.SetColumn(button, 1); content.Children.Add(button);
        Grid.SetRow(content, 1); panel.Children.Add(content);
        card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
    }

    private void UpdateBackupSummary(BackupInfo? backup)
    {
        if (_backupSummary is null)
            return;
        _backupSummary.Text = backup is null
            ? (IsGerman ? "Kein Backup vorhanden." : "No backup available.")
            : $"YACA {backup.Version}\n{backup.CreatedAt.ToLocalTime():g}";
    }

    private void RenderVersionList(YacaPluginInfo? current)
    {
        if (_versionList is null)
            return;
        _versionList.Children.Clear();
        var plugins = GetDistinctPlugins().OrderByDescending(plugin => plugin.Version).ThenByDescending(plugin => plugin.Build).ToList();
        foreach (var plugin in plugins)
        {
            var active = current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true;
            var row = new Grid { MinHeight = 34, Background = active ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(new TextBlock { Text = $"YACA {plugin.Version}", FontSize = DashboardVersionListFontSize, Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 2) });
            _versionList.Children.Add(row);
        }
        if (_versionsFooterText is not null)
            _versionsFooterText.Text = plugins.Count.ToString(CultureInfo.InvariantCulture);
    }

    private void UpdateCurrentInstalledDetailsFromTextFallback(string text)
    {
        UpdateCurrentInstalledDetailsFromText(text);
    }
}
