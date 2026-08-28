using System.Windows;
using System.Windows.Controls;

namespace YacaPluginSwitcher;

public static class VersionRowBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(VersionRowBehavior),
        new PropertyMetadata(false, OnChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not DockPanel panel || e.NewValue is not true)
            return;

        panel.Loaded -= Apply;
        panel.Loaded += Apply;
    }

    private static void Apply(object sender, RoutedEventArgs e)
    {
        if (sender is not DockPanel panel)
            return;

        foreach (var text in panel.Children.OfType<TextBlock>())
        {
            text.FontSize = 13;
            text.VerticalAlignment = VerticalAlignment.Center;
        }

        foreach (var badge in panel.Children.OfType<Border>())
        {
            if (badge.Child is not TextBlock text ||
                (!string.Equals(text.Text, "AKTUELL", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(text.Text, "CURRENT", StringComparison.OrdinalIgnoreCase)))
                continue;

            badge.MinWidth = 58;
            badge.Padding = new Thickness(7, 3, 7, 3);
            badge.Margin = new Thickness(10, 0, 0, 0);
            badge.CornerRadius = new CornerRadius(4);
            text.FontSize = 10;
            text.FontWeight = FontWeights.SemiBold;
            text.TextAlignment = TextAlignment.Center;
        }
    }
}
