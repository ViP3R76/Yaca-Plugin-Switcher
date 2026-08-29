using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private Grid? _versionPanelGrid;
    private TextBlock? _versionFooter;
    private bool _versionHeaderStyled;
    private string _versionPanelSignature = string.Empty;

    private void EnsureVersionPanelLayout()
    {
        if (_versionPanelGrid is not null && _versionPanelGrid is System.Windows.Media.Visual visual && visual.IsDescendantOf(PageHost))
            return;

        // ShowHome() recreates PageHost. Discard references to the old dashboard tree.
        _versionPanelGrid = null;
        _versionFooter = null;
        _versionHeaderStyled = false;
        _versionPanelSignature = string.Empty;
        _versionList = FindCurrentVersionList();

        if (_versionList is null || _versionList.Parent is not Grid host)
            return;

        host.Children.Remove(_versionList);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

        _versionList = new StackPanel
        {
            Margin = new Thickness(0, 4, 0, 0),
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetRow(_versionList, 0);
        content.Children.Add(_versionList);

        _versionFooter = new TextBlock
        {
            Foreground = (Brush)FindResource("AccentBrush"),
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 2, 0, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        _versionFooter.MouseLeftButtonUp += (_, _) => ShowSwitchPage();
        Grid.SetRow(_versionFooter, 1);
        content.Children.Add(_versionFooter);

        Grid.SetRow(content, 1);
        host.Children.Add(content);
        _versionPanelGrid = content;

        StyleVersionPanelHeader(host);
    }

    private StackPanel? FindCurrentVersionList()
    {
        foreach (var text in FindVisualTextBlocks(PageHost))
        {
            if (!text.Text.Contains("VERFÜGBARE YACA-VERSIONEN", StringComparison.OrdinalIgnoreCase)
                && !text.Text.Contains("AVAILABLE YACA VERSIONS", StringComparison.OrdinalIgnoreCase))
                continue;

            if (text.Parent is StackPanel headerPanel && headerPanel.Parent is Grid host)
            {
                var content = host.Children
                    .OfType<Grid>()
                    .FirstOrDefault(child => Grid.GetRow(child) == 1);
                var list = content?.Children
                    .OfType<StackPanel>()
                    .FirstOrDefault(child => Grid.GetRow(child) == 0);
                if (list is not null)
                    return list;
            }

            if (text.Parent is Grid directHost)
            {
                var list = directHost.Children
                    .OfType<StackPanel>()
                    .FirstOrDefault(child => Grid.GetRow(child) == 1);
                if (list is not null)
                    return list;
            }
        }

        return null;
    }

    private void StyleVersionPanelHeader(Grid host)
    {
        if (_versionHeaderStyled)
            return;

        var header = host.Children
            .OfType<TextBlock>()
            .FirstOrDefault(text => text.Text.Contains("VERFÜGBARE YACA-VERSIONEN", StringComparison.OrdinalIgnoreCase)
                                    || text.Text.Contains("AVAILABLE YACA VERSIONS", StringComparison.OrdinalIgnoreCase));

        if (header is null)
            return;

        host.Children.Remove(header);

        var headerPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerPanel.Children.Add(new Image
        {
            Source = LoadLogo(),
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });

        headerPanel.Children.Add(new TextBlock
        {
            Text = IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS",
            Foreground = (Brush)FindResource("AccentBrush"),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        Grid.SetRow(headerPanel, 0);
        host.Children.Add(headerPanel);
        _versionHeaderStyled = true;
    }

    private void RefreshVersionPanelLayout()
    {
        EnsureVersionPanelLayout();

        if (_versionList is null || _versionPanelGrid is null || _versionFooter is null)
            return;

        var host = _versionPanelGrid.Parent as Grid;
        if (host is not null)
            StyleVersionPanelHeader(host);

        var plugins = _plugins
            .OrderByDescending(plugin => plugin.Version)
            .ThenByDescending(plugin => plugin.Build ?? long.MinValue)
            .ThenByDescending(plugin => plugin.FileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var current = _service.DetectCurrent();
        var signature = string.Join("|", plugins.Select(plugin => $"{plugin.FilePath}:{plugin.Sha256}:{plugin.Version}:{plugin.Build}"));
        signature += $"|current:{current?.Sha256}";
        signature += $"|lang:{Localization.Normalize(_service.Settings.Language)}";

        if (string.Equals(signature, _versionPanelSignature, StringComparison.Ordinal))
            return;

        _versionPanelSignature = signature;
        _versionList.Children.Clear();

        if (plugins.Count == 0)
        {
            _versionList.Children.Add(new TextBlock
            {
                Text = IsGerman ? "Keine gültigen YACA-Versionen gefunden." : "No valid YACA versions found.",
                FontSize = 13,
                Foreground = (Brush)FindResource("SecondaryBrush"),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
        else
        {
            var availableHeight = _versionPanelGrid.ActualHeight > 0
                ? Math.Max(20, _versionPanelGrid.ActualHeight - 30)
                : 210;
            var rowHeight = Math.Clamp(availableHeight / plugins.Count, 20, 36);

            foreach (var plugin in plugins)
            {
                var active = current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true;
                var row = new Grid
                {
                    Height = rowHeight,
                    MinHeight = 20,
                    Margin = new Thickness(0)
                };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                row.Children.Add(new Border
                {
                    BorderBrush = (Brush)FindResource("BorderBrush"),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Opacity = 0.55,
                    VerticalAlignment = VerticalAlignment.Bottom
                });

                var name = new TextBlock
                {
                    Text = plugin.DisplayName,
                    FontSize = rowHeight <= 23 ? 12 : 13,
                    Foreground = (Brush)FindResource("ForegroundBrush"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(name, 0);
                row.Children.Add(name);

                if (active)
                {
                    var badge = new Border
                    {
                        Background = (Brush)FindResource("SuccessBrush"),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(6, 1, 6, 1),
                        Margin = new Thickness(8, 0, 0, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = IsGerman ? "AKTUELL" : "CURRENT",
                            Foreground = Brushes.Black,
                            FontSize = 8.5,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center
                        }
                    };
                    Grid.SetColumn(badge, 1);
                    row.Children.Add(badge);
                }

                _versionList.Children.Add(row);
            }
        }

        _versionFooter.Text = IsGerman
            ? $"{plugins.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} Version(en) verfügbar  ›"
            : $"{plugins.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)} version(s) available  ›";
    }
}
