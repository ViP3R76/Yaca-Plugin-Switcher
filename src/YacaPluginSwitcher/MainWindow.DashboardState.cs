using System.Windows.Controls;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    // Shared dashboard state. DashboardRenderer creates and owns the visual;
    // DashboardRenderer.Behaviors only updates it after the renderer has handed it over.
    private Image? _teamSpeakStatusIcon;
}
