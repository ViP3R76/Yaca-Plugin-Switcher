using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void BuildBrandingPanel(Grid host, int column)
    {
        var card = CreatePanelCard((Brush)FindResource("AccentBrush"));
        var branding = new Image
        {
            Source = new BitmapImage(new Uri("/YacaPluginSwitcher;component/Assets/branding_logo.png", UriKind.RelativeOrAbsolute)),
            Width = 230,
            Height = 230,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        card.Child = branding;
        Grid.SetColumn(card, column);
        host.Children.Add(card);
    }
}
