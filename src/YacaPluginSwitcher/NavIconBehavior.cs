using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public static class NavIconBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(NavIconBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnIsEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Button button || e.NewValue is not true)
            return;

        button.Loaded -= ApplyIcon;
        button.Loaded += ApplyIcon;
    }

    private static void ApplyIcon(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Content is not StackPanel panel || panel.Children.Count == 0)
            return;

        if (panel.Children[0] is not TextBlock icon)
            return;

        icon.FontFamily = new FontFamily("Segoe MDL2 Assets");
        icon.FontSize = 19;
        icon.TextAlignment = TextAlignment.Center;
        icon.VerticalAlignment = VerticalAlignment.Center;
        icon.Width = 34;
        icon.Text = button.Tag?.ToString() switch
        {
            "home" => "\uE80F",
            "refresh" => "\uE72C",
            "switch" => "\uE895",
            "backup-create" => "\uE74E",
            "backups" => "\uE8D5",
            "config" => "\uE713",
            "info" => "\uEA1F",
            "exit" => "\uE7E8",
            _ => icon.Text
        };
    }
}
