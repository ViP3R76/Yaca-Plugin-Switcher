using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SharpVectors.Converters;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private static void ConfigureNavContent(StackPanel content, Button owner, string iconAssetKey, string text, Brush? iconBrush = null)
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
            svgIcon.SetBinding(SvgIcon.FillProperty, new Binding("Foreground")
            {
                Source = owner,
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

    private void AddNav(string key, string iconAssetKey, string text, Action action)
    {
        var button = new Button
        {
            Style = (Style)FindResource("NavButtonStyle"),
            Height = 46,
            Tag = key
        };
        var content = new StackPanel();
        ConfigureNavContent(content, button, iconAssetKey, text);
        button.Content = content;
        button.Click += (_, _) => action();
        NavPanel.Children.Add(button);
        _navButtons.Add((key, button));
    }

    private void SetActiveNav(string key)
    {
        _activePage = key;
        foreach (var item in _navButtons)
        {
            var selected = item.Key.Equals(key, StringComparison.OrdinalIgnoreCase);
            item.Button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
            item.Button.BorderBrush = Brushes.Transparent;
            item.Button.BorderThickness = new Thickness(0);
        }
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
