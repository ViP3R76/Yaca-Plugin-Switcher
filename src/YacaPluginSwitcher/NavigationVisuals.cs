using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyReferenceIcons();
    }

    private void ApplyReferenceIcons()
    {
        var iconFont = new FontFamily("Segoe MDL2 Assets");

        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (button.Content is not StackPanel stack || stack.Children.Count == 0 || stack.Children[0] is not TextBlock icon)
                continue;

            var key = button.Tag as string;
            icon.Text = key switch
            {
                "home" => "\uE80F",
                "refresh" => "\uE895",
                "switch" => "\uE8AB",
                "backup-create" => "\uE710",
                "backups" => "\uE8F1",
                "config" => "\uE713",
                "info" => "\uE946",
                _ => icon.Text
            };
            icon.FontFamily = iconFont;
            icon.FontSize = 24;
            icon.TextAlignment = TextAlignment.Center;
            icon.Margin = new Thickness(0, 0, 12, 0);
        }

        foreach (var button in FindVisualChildren<Button>(PageHost))
        {
            if (button.Content is not StackPanel stack || stack.Children.Count < 2 || stack.Children[0] is not TextBlock icon || stack.Children[1] is not TextBlock title)
                continue;

            var glyph = title.Text switch
            {
                "YACA WECHSELN" => "\uE8AB",
                "BACKUP ERSTELLEN" => "\uE710",
                "YACA UPDATER" => "\uE753",
                _ => null
            };

            if (glyph is null)
                continue;

            icon.Text = glyph;
            icon.FontFamily = iconFont;
            icon.FontSize = 48;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null)
            yield break;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
                yield return match;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }
}
