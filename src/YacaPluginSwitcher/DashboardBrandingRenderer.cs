using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void BuildBrandingPanel(Grid host, int column)
    {
        var card = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            Margin = new Thickness(6)
        };

        var branding = new Image
        {
            Source = new BitmapImage(new Uri("/YacaPluginSwitcher;component/Assets/branding_logo.png", UriKind.RelativeOrAbsolute))
            {
                CacheOption = BitmapCacheOption.OnLoad
            },
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 520,
            MaxHeight = DashboardPanelHeight - 28,
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            IsHitTestVisible = false
        };

        RenderOptions.SetBitmapScalingMode(branding, BitmapScalingMode.HighQuality);
        RenderOptions.SetEdgeMode(branding, EdgeMode.Unspecified);

        card.Child = branding;
        Grid.SetColumn(card, column);
        host.Children.Add(card);
    }
}
