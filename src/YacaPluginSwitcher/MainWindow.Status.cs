using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Aktualisiert den globalen Statusbereich im Footer.
    /// </summary>
    private void SetGlobalStatus(string message, bool success = false)
    {
        GlobalFooterStatusText.Text = message;
        GlobalFooterStatusText.Foreground =
            (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush");
        GlobalFooterStatusText.FontWeight =
            success ? FontWeights.Bold : FontWeights.Normal;
        GlobalFooterStatusText.TextWrapping = TextWrapping.NoWrap;
        GlobalFooterStatusText.TextTrimming = TextTrimming.None;
        GlobalFooterStatusText.TextAlignment = TextAlignment.Center;
        GlobalFooterStatusText.VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>
    /// Zeigt einen erfolgreichen Pluginwechsel als eindeutige einzeilige Statusmeldung an.
    /// </summary>
    private void SetPluginSwitchFooterStatus(YacaPluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";

        SetGlobalStatus(
            IsGerman
                ? $"Plugin gewechselt auf: Yaca {plugin.Version} Punkt (Build: {build})"
                : $"Plugin switched to: Yaca {plugin.Version} Point (Build: {build})",
            success: true);
    }

    /// <summary>
    /// Zeigt einen Fehler im globalen Footerstatus an.
    /// </summary>
    private void ShowError(string message)
    {
        SetGlobalStatus(message);
    }
}
