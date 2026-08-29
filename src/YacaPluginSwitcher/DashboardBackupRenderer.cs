using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void BuildLatestBackupPanel(Grid host, int column)
    {
        var card = CreatePanelCard((Brush)FindResource("BorderBrush")); _backupCard = card;
        var panel = new Grid(); panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var header = CreateDashboardHeader("backups", IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP"); Grid.SetRow(header, 0); panel.Children.Add(header);
        var content = new Grid { Margin = new Thickness(6, 16, 6, 0) }; content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        _backupSummary = new TextBlock { FontSize = 15, LineHeight = 22, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 10, 0) }; Grid.SetColumn(_backupSummary, 0); content.Children.Add(_backupSummary);
        var folderIcon = CreateBackupFolderIcon((Brush)FindResource("AccentBrush")); Grid.SetColumn(folderIcon, 1); content.Children.Add(folderIcon); Grid.SetRow(content, 1); panel.Children.Add(content); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
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
            var plugin = ordered[index];
            var row = new Border { Background = GetVersionRowBackground(index), CornerRadius = new CornerRadius(3), Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 1, 0, 1) };
            var grid = new Grid { MinHeight = 34 }; grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Children.Add(new TextBlock { Text = $"YACA {plugin.Version} - (Build: {plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"})", FontSize = DashboardVersionListFontSize, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
            if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true)
            {
                var badge = new Border { Background = (Brush)FindResource("SuccessBrush"), CornerRadius = new CornerRadius(4), Padding = new Thickness(7, 2, 7, 2), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0), Child = new TextBlock { Text = IsGerman ? "INSTALLIERT" : "INSTALLED", Foreground = Brushes.Black, FontSize = 10, FontWeight = FontWeights.Bold } }; Grid.SetColumn(badge, 1); grid.Children.Add(badge);
            }
            row.Child = grid; _versionList.Children.Add(row);
        }
        if (_versionsFooterText is not null) _versionsFooterText.Text = IsGerman ? $"{_plugins.Count.ToString(CultureInfo.InvariantCulture)} Version(en) verfügbar" : $"{_plugins.Count.ToString(CultureInfo.InvariantCulture)} version(s) available";
    }

    private void UpdateBackupSummary(BackupInfo? backup)
    {
        if (_backupSummary is null) return;
        _backupSummary.Inlines.Clear();
        if (backup is null) { _backupSummary.Inlines.Add(new Run(Texts.NoBackups)); return; }

        var versionText = !string.IsNullOrWhiteSpace(backup.SourceVersion)
            ? $"YACA {backup.SourceVersion}"
            : backup.DisplayName.Split(" - ", 2, StringSplitOptions.None)[0];
        var statusText = backup.IsAutomatic ? (IsGerman ? "Automatisch" : "Automatic") : (IsGerman ? "Manuell" : "Manual");
        var buildText = backup.SourceBuild?.ToString(CultureInfo.InvariantCulture) ?? "—";

        _backupSummary.Inlines.Add(new Run($"{backup.Timestamp:dd.MM.yyyy HH:mm}") { FontSize = 34, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("GoldBrush") });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run(" ") { FontSize = 8 });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run(versionText) { FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("ForegroundBrush") });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run("────────────────────────") { FontSize = 7, Foreground = (Brush)FindResource("BorderBrush") });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"Build: {buildText}  •  {statusText}") { FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("ForegroundBrush") });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"{(IsGerman ? "Datei" : "File")}  -  {backup.FileName}") { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush") });
        _backupSummary.Inlines.Add(new LineBreak());
        _backupSummary.Inlines.Add(new Run($"{(IsGerman ? "Größe" : "Size")}  -  {backup.FileSize / 1024d / 1024d:0.00} MB") { FontSize = 15, Foreground = (Brush)FindResource("ForegroundBrush") });
    }

    private static Grid CreateBackupFolderIcon(Brush brush)
    {
        var canvas = new Grid { Width = 150, Height = 150, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        canvas.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 13 31 L 13 24 C 13 20 16 18 20 18 H 36 L 45 27 H 82 C 87 27 90 30 90 35 V 69 C 90 74 87 77 82 77 H 18 C 13 77 10 74 10 69 V 31 C 10 26 13 24 18 24"), Stroke = brush, StrokeThickness = 3.4, Fill = Brushes.Transparent, Stretch = Stretch.Uniform, Width = 126, Height = 104, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(8, 10, 0, 0) });
        canvas.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 83 70 A 25 25 0 1 1 76 42"), Stroke = brush, StrokeThickness = 3.4, Fill = Brushes.Transparent, Stretch = Stretch.Uniform, Width = 70, Height = 70, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 6, 8) });
        canvas.Children.Add(new System.Windows.Shapes.Path { Data = Geometry.Parse("M 73 39 L 78 49 L 88 44"), Stroke = brush, StrokeThickness = 3.4, Fill = Brushes.Transparent, Stretch = Stretch.Uniform, Width = 34, Height = 34, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 44) });
        return canvas;
    }

    private static ControlTemplate CreateSquareButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
        border.AppendChild(presenter); template.VisualTree = border; return template;
    }
}
