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
        var header = CreateDashboardHeader("backup", IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP"); Grid.SetRow(header, 0); panel.Children.Add(header);
        var content = new Grid { Margin = new Thickness(6, 16, 6, 0) }; content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        _backupSummary = new TextBlock { FontSize = 15, LineHeight = 22, Foreground = (Brush)FindResource("ForegroundBrush"), TextWrapping = TextWrapping.NoWrap, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left, Margin = new Thickness(0, 0, 10, 0) }; Grid.SetColumn(_backupSummary, 0); content.Children.Add(_backupSummary);
        var backupIcon = CreateIcon(DashboardIconData["backup"], (Brush)FindResource("AccentBrush"), 132, 132, 3.6); Grid.SetColumn(backupIcon, 1); content.Children.Add(backupIcon); Grid.SetRow(content, 1); panel.Children.Add(content); card.Child = panel; Grid.SetColumn(card, column); host.Children.Add(card);
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

        var versionText = backup.DisplayName.Split(" - ", 2, StringSplitOptions.None)[0];
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
