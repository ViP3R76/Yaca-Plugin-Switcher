using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private Image? _teamSpeakStatusIcon;

    /// <summary>
    /// Renderer-owned TeamSpeak status update. The behavior layer only signals
    /// status changes; asset selection and visual state remain in the renderer.
    /// </summary>
    private void UpdateTeamSpeakStatusIcon()
    {
        if (_teamSpeakStatusIcon is null || _tsStatus is null)
            return;

        var running = _tsStatus.Text.Equals("GESTARTET", StringComparison.OrdinalIgnoreCase)
            || _tsStatus.Text.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);

        DashboardIconRegistry.SetAsset(
            _teamSpeakStatusIcon,
            running
                ? DashboardIconRegistry.IconAssetTeamSpeakStarted
                : DashboardIconRegistry.IconAssetTeamSpeakStopped);

        DashboardIconRegistry.SetFill(
            _teamSpeakStatusIcon,
            running
                ? (Brush)FindResource("ErrorBrush")
                : (Brush)FindResource("SuccessBrush"));
    }
}
