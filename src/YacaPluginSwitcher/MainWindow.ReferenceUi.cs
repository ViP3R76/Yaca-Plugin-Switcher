using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyReferenceNavigationIcons();
        ApplyReferencePanelIcons();
    }

    private void ApplyReferenceNavigationIcons()
    {
        var icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["home"] = "M 3,11 L 12,3 L 21,11 L 21,21 L 15,21 L 15,14 L 9,14 L 9,21 L 3,21 Z",
            ["refresh"] = "M 20,11 A 8,8 0 1 0 18,16 M 20,5 L 20,11 L 14,11",
            ["switch"] = "M 3,8 L 21,8 M 16,3 L 21,8 L 16,13 M 21,16 L 3,16 M 8,11 L 3,16 L 8,21",
            ["backup-create"] = "M 4,4 L 20,4 L 20,20 L 4,20 Z M 12,8 L 12,16 M 8,12 L 16,12",
            ["backups"] = "M 4,5 C 4,2 20,2 20,5 L 20,19 C 20,22 4,22 4,19 Z M 4,5 C 4,8 20,8 20,5 M 4,12 C 4,15 20,15 20,12",
            ["config"] = "M 12,3 L 13.5,5.5 L 16.5,5 L 18,7.5 L 21,8 L 20.5,11 L 22,13 L 20.5,15 L 21,18 L 18,18.5 L 16.5,21 L 13.5,20.5 L 12,23 L 10.5,20.5 L 7.5,21 L 6,18.5 L 3,18 L 3.5,15 L 2,13 L 3.5,11 L 3,8 L 6,7.5 L 7.5,5 L 10.5,5.5 Z M 12,9 A 4,4 0 1 0 12,17 A 4,4 0 1 0 12,9",
            ["info"] = "M 12,2 A 10,10 0 1 0 12,22 A 10,10 0 1 0 12,2 M 12,10 L 12,17 M 12,6 L 12,7"
        };

        foreach (var child in NavPanel.Children)
        {
            if (child is not Button button || button.Tag is not string key || !icons.TryGetValue(key, out var data))
                continue;
            if (button.Content is not StackPanel panel || panel.Children.Count == 0)
                continue;

            var icon = CreateIcon(data, (Brush)FindResource(key == "home" ? "GoldBrush" : "ForegroundBrush"), 25, 25, 2.1);
            panel.Children[0] = icon;
        }
    }

    private void ApplyReferencePanelIcons()
    {
        var iconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["YACA WECHSELN"] = "M 12,4 L 21,4 M 17,0 L 21,4 L 17,8 M 21,12 L 3,12 M 7,8 L 3,12 L 7,16",
            ["BACKUP ERSTELLEN"] = "M 4,4 L 20,4 L 20,20 L 4,20 Z M 12,7 L 12,17 M 7,12 L 17,12",
            ["YACA UPDATER"] = "M 7,17 L 17,17 A 5,5 0 0 0 18,8 A 7,7 0 0 0 5,9 A 4,4 0 0 0 7,17 M 12,11 L 12,20 M 8,16 L 12,20 L 16,16"
        };

        foreach (var text in FindVisualTextBlocks(PageHost))
        {
            if (!iconMap.TryGetValue(text.Text.Trim(), out var data) || text.Parent is not Panel parent || parent.Children.Count == 0)
                continue;
            var accent = text.Text.Contains("BACKUP", StringComparison.OrdinalIgnoreCase) ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("AccentBrush");
            parent.Children[0] = CreateIcon(data, accent, 82, 82, 4.0);
        }
    }

    private static Path CreateIcon(string data, Brush stroke, double width, double height, double thickness) => new()
    {
        Data = Geometry.Parse(data),
        Stroke = stroke,
        StrokeThickness = thickness,
        Fill = Brushes.Transparent,
        Width = width,
        Height = height,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static IEnumerable<TextBlock> FindVisualTextBlocks(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is TextBlock text)
                yield return text;
            if (child is DependencyObject dependency)
            {
                foreach (var nested in FindVisualTextBlocks(dependency))
                    yield return nested;
            }
        }
    }
}
