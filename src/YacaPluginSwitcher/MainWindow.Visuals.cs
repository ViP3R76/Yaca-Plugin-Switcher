using System.Windows.Media;
using System.Windows.Controls;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void NavButton_Loaded(object sender, RoutedEventArgs e)
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
