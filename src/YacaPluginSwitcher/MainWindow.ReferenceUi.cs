using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void ApplyReferenceVisuals(object sender, RoutedEventArgs e)
    {
        ApplyReferenceNavigationLayout();
        ApplyReferenceNavigationIcons();
        ApplyReferencePanelIcons();
        ApplyReferenceTileStyles();
        ApplyReferenceDashboardGeometry();
        RemoveUpdaterBadge();
        if (!ReferencePanelRefreshHooked)
        {
            DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl))?.AddValueChanged(PageHost, (_, _) => Dispatcher.BeginInvoke(new Action(() =>
            {
                ApplyReferencePanelIcons(); ApplyReferenceTileStyles(); RemoveUpdaterBadge();
            })));
            ReferencePanelRefreshHooked = true;
        }
    }

    private bool ReferencePanelRefreshHooked { get; set; }

    private void ApplyReferenceNavigationLayout()
    {
        if (NavPanel.Children.Count > 0 && NavPanel.Children.OfType<Button>().Any(b => b.Tag is string tag && tag.Equals("updater", StringComparison.OrdinalIgnoreCase))) return;
        NavPanel.Children.Clear(); _navButtons.Clear();
        AddNav("home", "⌂", "Dashboard", ShowHome);
        AddNav("refresh", "↻", IsGerman ? "Aktualisieren" : "Refresh", () => RefreshActivePage(true));
        AddNav("switch", "⇄", IsGerman ? "YACA wechseln" : "Switch YACA", () => ShowSwitchPage());
        AddNav("updater", "☁", "YACA Updater", () => ShowError(IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version."));
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("backup-create", "＋", IsGerman ? "Backup erstellen" : "Create Backup", CreateBackupFromDashboard);
        AddNav("backups", "▣", IsGerman ? "Backup verwalten" : "Manage Backups", ShowBackups);
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("info", "ⓘ", IsGerman ? "Info & Links" : "Info & Links", ShowInfo);
    }

    private void ApplyReferenceNavigationIcons()
    {
        var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "M 3,10 L 12,3 L 21,10 L 21,21 L 15,21 L 15,14 L 9,14 L 9,21 L 3,21 Z",
            ["refresh"] = "M 20,11 A 8,8 0 1 0 18,16 M 20,5 L 20,11 L 14,11",
            ["switch"] = "M 3,8 L 21,8 M 16,3 L 21,8 L 16,13 M 21,16 L 3,16 M 8,11 L 3,16 L 8,21",
            ["updater"] = "M 7,17 L 17,17 A 5,5 0 0 0 18,8 A 7,7 0 0 0 5,9 A 4,4 0 0 0 7,17 M 12,11 L 12,20 M 8,16 L 12,20 L 16,16",
            ["backup-create"] = "M 4,4 L 20,4 L 20,20 L 4,20 Z M 12,8 L 12,16 M 8,12 L 16,12",
            ["backups"] = "M 4,5 C 4,2 20,2 20,5 L 20,19 C 20,22 4,22 4,19 Z M 4,5 C 4,8 20,8 20,5 M 4,12 C 4,15 20,15 20,12",
            ["info"] = "M 12,2 A 10,10 0 1 0 12,22 A 10,10 0 1 0 12,2 M 12,10 L 12,17 M 12,6 L 12,7"
        };
        foreach (var child in NavPanel.Children)
        {
            if (child is not Button button || button.Tag is not string key || !icons.TryGetValue(key, out var data) || button.Content is not StackPanel panel || panel.Children.Count == 0) continue;
            var accent = key.Equals("home", StringComparison.OrdinalIgnoreCase) ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
            var icon = CreateIcon(data, accent, 30, 30, 2.35); icon.Margin = new Thickness(0, 0, 12, 0);
            panel.Children.RemoveAt(0); panel.Children.Insert(0, icon);
            button.MouseEnter -= NavButton_MouseEnter; button.MouseLeave -= NavButton_MouseLeave; button.MouseEnter += NavButton_MouseEnter; button.MouseLeave += NavButton_MouseLeave;
        }
        ApplyReferenceDashboardGeometry();
    }

    private void NavButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e) { if (sender is Button b) { b.Background = (Brush)FindResource("NavSelectedBrush"); b.Foreground = (Brush)FindResource("GoldBrush"); b.BorderBrush = (Brush)FindResource("GoldBrush"); b.BorderThickness = new Thickness(1); } }
    private void NavButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e) { if (sender is not Button b) return; var selected = b.Tag is string key && _navButtons.Any(x => x.Button == b && x.Key.Equals(_activePage, StringComparison.OrdinalIgnoreCase)); b.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent; b.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"); b.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent; b.BorderThickness = selected ? new Thickness(1) : new Thickness(0); }

    private void ApplyReferencePanelIcons()
    {
        var iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["YACA WECHSELN"] = "M 12,3 A 9,9 0 1 0 21,12 A 9,9 0 1 0 12,21 M 12,3 L 16,7 M 12,3 L 8,7 M 3,12 L 7,16 M 21,12 L 17,8",
            ["BACKUP ERSTELLEN"] = "M 4,4 L 20,4 L 20,20 L 4,20 Z M 12,7 L 12,17 M 7,12 L 17,12",
            ["YACA UPDATER"] = "M 6,17 L 18,17 A 5,5 0 0 0 18,8 A 7,7 0 0 0 5,9 A 4,4 0 0 0 6,17 M 12,10 L 12,20 M 8,16 L 12,20 L 16,16"
        };
        foreach (var text in FindVisualTextBlocks(PageHost).ToList())
        {
            var normalized = text.Text.Trim(); if (!iconMap.TryGetValue(normalized, out var data) || text.Parent is not Panel parent || parent.Children.Count == 0) continue;
            var accent = normalized.Contains("BACKUP", StringComparison.OrdinalIgnoreCase) ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("AccentBrush");
            var icon = CreateIcon(data, accent, 92, 92, 3.8); parent.Children.RemoveAt(0); parent.Children.Insert(0, icon);
        }
        ApplyReferenceTileStyles();
    }

    private void RemoveUpdaterBadge()
    {
        foreach (var text in FindVisualTextBlocks(PageHost).Where(t => t.Text.Contains("BALD", StringComparison.OrdinalIgnoreCase) || t.Text.Contains("COMING", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (text.Parent is Border badge && badge.Parent is Panel parent) parent.Children.Remove(badge); else text.Visibility = Visibility.Collapsed;
        }
    }

    private void ApplyReferenceTileStyles()
    {
        var referenceStyle = (Style)FindResource("TileButtonStyle"); var referenceTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "YACA WECHSELN", "BACKUP ERSTELLEN", "YACA UPDATER" };
        foreach (var button in FindVisualButtons(PageHost).ToList()) if ((button.Tag is string tag && tag.Equals("reference-dashboard-tile", StringComparison.OrdinalIgnoreCase)) || FindVisualTextBlocks(button).Any(t => referenceTitles.Contains(t.Text.Trim()))) button.Style = referenceStyle;
    }

    private void ApplyReferenceDashboardGeometry()
    {
        if (PageHost.Content is not Grid root || root.RowDefinitions.Count < 3) return;
        root.RowDefinitions[0].Height = new GridLength(286); root.RowDefinitions[1].Height = new GridLength(286);
        var top = root.Children.OfType<Grid>().FirstOrDefault(child => Grid.GetRow(child) == 0); if (top is null) return;
        ApplyCurrentInstalledPanel();
        var logo = top.Children.OfType<Border>().Select(border => new { Border = border, Image = border.Child as Image }).FirstOrDefault(x => x.Image is not null); if (logo is null) return;
        var column = Grid.GetColumn(logo.Border); top.Children.Remove(logo.Border); var freeLogo = new Image { Source = LoadLogo(), Width = 260, Height = 260, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false }; Grid.SetColumn(freeLogo, column); top.Children.Add(freeLogo);
    }

    private void ApplyCurrentInstalledPanel()
    {
        if (_currentCard is null) return; if (_currentCard.Child is Grid existing && existing.Tag is string tag && tag.Equals("reference-current-panel", StringComparison.OrdinalIgnoreCase)) return;
        var current = _service.DetectCurrent(); var oldValue = _currentValue?.Text ?? "—"; var grid = new Grid { Tag = "reference-current-panel" };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) }); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(96) });
        var header = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; header.Children.Add(CreateIcon("M 12,2 L 20,5 L 20,11 C 20,16 16.8,20 12,22 C 7.2,20 4,16 4,11 L 4,5 Z", (Brush)FindResource("AccentBrush"), 28, 28, 2.25)); header.Children.Add(new TextBlock { Text = IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", Foreground = (Brush)FindResource("AccentBrush"), FontSize = 20, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) }); Grid.SetRow(header, 0); grid.Children.Add(header);
        var center = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; _currentValue = new TextBlock { Text = current?.Version?.ToString() ?? oldValue, FontSize = 38, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center }; center.Children.Add(_currentValue); center.Children.Add(new Border { Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(14, 4, 14, 4), Margin = new Thickness(0, 12, 0, 0), HorizontalAlignment = HorizontalAlignment.Center, Child = new TextBlock { Text = IsGerman ? "AKTIV" : "ACTIVE", Foreground = Brushes.Black, FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center } }); Grid.SetRow(center, 1); grid.Children.Add(center);
        _currentDetails = new TextBlock { Text = FormatCurrentInstalledDetails(current), FontSize = 13, LineHeight = 20, Foreground = (Brush)FindResource("SecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Left, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 0, 12, 0) }; Grid.SetRow(_currentDetails, 2); grid.Children.Add(_currentDetails); _currentCard.Child = grid;
    }

    private string FormatCurrentInstalledDetails(YacaPluginSwitcher.Models.YacaPluginInfo? current)
    {
        if (current is null) return string.Empty; var build = current.Build?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "—"; var size = $"{current.FileSize / 1024d / 1024d:0.00} MB"; var sha = current.Sha256; var file = System.IO.Path.GetFileName(current.FilePath); return IsGerman ? $"Build: {build}\nGröße: {size}\nSHA-256: {sha}\nDatei: {file}" : $"Build: {build}\nSize: {size}\nSHA-256: {sha}\nFile: {file}";
    }

    private static System.Windows.Shapes.Path CreateIcon(string data, Brush stroke, double width, double height, double thickness) => new() { Data = Geometry.Parse(data), Stroke = stroke, StrokeThickness = thickness, Fill = Brushes.Transparent, Width = width, Height = height, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private static IEnumerable<TextBlock> FindVisualTextBlocks(DependencyObject root) { foreach (var child in LogicalTreeHelper.GetChildren(root)) { if (child is TextBlock text) yield return text; if (child is DependencyObject dependency) foreach (var nested in FindVisualTextBlocks(dependency)) yield return nested; } }
    private static IEnumerable<Button> FindVisualButtons(DependencyObject root) { foreach (var child in LogicalTreeHelper.GetChildren(root)) { if (child is Button button) yield return button; if (child is DependencyObject dependency) foreach (var nested in FindVisualButtons(dependency)) yield return nested; } }
}