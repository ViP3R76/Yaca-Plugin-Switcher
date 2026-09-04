using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void SetGlobalStatus(string message, bool success = false)
    {
        GlobalFooterStatusText.Text = message;
        GlobalFooterStatusText.Foreground = (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush");
        GlobalFooterStatusText.FontWeight = success ? FontWeights.Bold : FontWeights.Normal;
        GlobalFooterStatusText.TextWrapping = TextWrapping.NoWrap;
        GlobalFooterStatusText.TextTrimming = TextTrimming.None;
        GlobalFooterStatusText.TextAlignment = TextAlignment.Center;
        GlobalFooterStatusText.VerticalAlignment = VerticalAlignment.Center;
    }

    private void SetPluginSwitchFooterStatus(YacaPluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";
        SetGlobalStatus(
            IsGerman
                ? $"Plugin gewechselt auf: Yaca {plugin.Version} · Build: {build}"
                : $"Plugin switched to: Yaca {plugin.Version} · Build: {build}",
            success: true);
    }

    private void ShowError(string message) => SetGlobalStatus(message);
}
