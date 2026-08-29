using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void ApplyDashboardBranding()
    {
        if (PageHost.Content is not Grid root || root.RowDefinitions.Count == 0)
            return;

        if (root.Children.OfType<Grid>().FirstOrDefault() is not Grid top || top.ColumnDefinitions.Count < 3)
            return;

        foreach (var existing in top.Children.OfType<Image>().Where(i => string.Equals(i.Tag as string, "vip3r-dashboard-branding", StringComparison.OrdinalIgnoreCase)).ToList())
            top.Children.Remove(existing);

        var branding = new Image
        {
            Source = new BitmapImage(new Uri("/YacaPluginSwitcher;component/Assets/vip3r_76_logo.png", UriKind.RelativeOrAbsolute)),
            Width = 200,
            Height = 200,
            Stretch = System.Windows.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "vip3r-dashboard-branding",
            IsHitTestVisible = false
        };

        Grid.SetColumn(branding, 1);
        Grid.SetRow(branding, 0);
        top.Children.Add(branding);
    }
}
