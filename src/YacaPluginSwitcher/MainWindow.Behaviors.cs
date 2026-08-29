using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void ApplyNavigationHover()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            button.MouseEnter -= Navigation_MouseEnter;
            button.MouseLeave -= Navigation_MouseLeave;
            button.MouseEnter += Navigation_MouseEnter;
            button.MouseLeave += Navigation_MouseLeave;
        }
    }

    private void Navigation_MouseEnter(object? sender, MouseEventArgs e)
    {
        if (sender is not Button button) return;
        button.Background = (Brush)FindResource("NavSelectedBrush");
        button.Foreground = (Brush)FindResource("GoldBrush");
        button.BorderBrush = (Brush)FindResource("GoldBrush");
        button.BorderThickness = new System.Windows.Thickness(1);
        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, (Brush)FindResource("GoldBrush"));
    }

    private void Navigation_MouseLeave(object? sender, MouseEventArgs e)
    {
        if (sender is not Button button) return;
        var selected = string.Equals(button.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase);
        button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
        button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
        button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
        button.BorderThickness = selected ? new System.Windows.Thickness(1) : new System.Windows.Thickness(0);
        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"));
    }
}
