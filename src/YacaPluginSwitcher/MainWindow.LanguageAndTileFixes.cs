using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    // Explicit overload for page handlers that accept an optional status message.
    // This prevents method-group conversion errors when passing ShowSwitchPage to AddTile.
    private void AddTile(
        Grid host,
        int column,
        string icon,
        string title,
        string subtitle,
        Brush accent,
        Action<string?> action,
        bool coming = false)
    {
        var button = new Button
        {
            Style = (Style)FindResource("TileButtonStyle"),
            BorderBrush = accent,
            Margin = new Thickness(6)
        };

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = icon,
            FontSize = 55,
            Foreground = accent,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 14,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            TextAlignment = TextAlignment.Center
        });

        if (coming)
        {
            var tileGrid = new Grid();
            tileGrid.Children.Add(panel);
            tileGrid.Children.Add(new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = (Brush)FindResource("NavSelectedBrush"),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(11, 5, 11, 5),
                Margin = new Thickness(0, -2, -2, 0),
                Child = new TextBlock
                {
                    Text = IsGerman ? "BALD\nVERFÜGBAR" : "COMING\nSOON",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center
                }
            });
            button.Content = tileGrid;
        }
        else
        {
            button.Content = panel;
        }

        button.Click += (_, _) => action(null);
        Grid.SetColumn(button, column);
        host.Children.Add(button);
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedIndex < 0)
            return;

        var language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;

        if (string.Equals(Localization.Normalize(_service.Settings.Language), language, StringComparison.OrdinalIgnoreCase))
            return;

        _service.Settings.Language = language;
        _service.Settings.Save();
        BuildNavigation();
        LoadLanguageSelector();
        ShowCurrentPageAfterLanguageChange();
    }

    private void ShowCurrentPageAfterLanguageChange()
    {
        switch (_activePage)
        {
            case "switch":
                ShowSwitchPage();
                break;
            case "backups":
                ShowBackups();
                break;
            case "config":
                ShowConfig();
                break;
            case "info":
                ShowInfo();
                break;
            default:
                ShowHome();
                break;
        }
    }
}
