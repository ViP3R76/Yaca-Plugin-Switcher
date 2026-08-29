using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private Image? _teamSpeakStatusIcon;

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

        _teamSpeakStatusIcon.HorizontalAlignment = HorizontalAlignment.Left;
        _teamSpeakStatusIcon.VerticalAlignment = VerticalAlignment.Center;
        _teamSpeakStatusIcon.Margin = new Thickness(17, 0, 0, 0);
    }
}
