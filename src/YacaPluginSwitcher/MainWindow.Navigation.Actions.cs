using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SharpVectors.Converters;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Erstellt den Inhalt eines Navigationsbuttons.
    /// Die SVG-Füllfarbe folgt zentral dem Foreground des zugehörigen Buttons.
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
        if (icon is SvgIcon svgIcon)
        {
            // Die Bindung ist die zentrale Farbquelle für alle SVG-Navigationsicons.
            svgIcon.SetBinding(SvgIcon.FillProperty, new Binding("Foreground")
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1),
                Mode = BindingMode.OneWay
            });
        }

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

            item.Button.Background = selected
                ? (Brush)FindResource("NavSelectedBrush")
                : Brushes.Transparent;
            item.Button.Foreground = selected
                ? (Brush)FindResource("GoldBrush")
                : (Brush)FindResource("ForegroundBrush");
            item.Button.BorderBrush = selected
                ? (Brush)FindResource("GoldBrush")
                : Brushes.Transparent;
            item.Button.BorderThickness = selected
                ? new Thickness(1)
                : new Thickness(0);
        }
    }

    /// <summary>
    /// Zeigt die aktuell ausgewählte Seite nach einem Sprachwechsel erneut an.
    /// </summary>
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
