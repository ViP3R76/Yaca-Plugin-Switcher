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
        GlobalFooterStatusText.TextAlignment = TextAlignment.Center;
    }

    /// <summary>
    /// Zeigt nach einem erfolgreichen Pluginwechsel den neuen YACA-Stand an.
    /// </summary>
    private void SetPluginSwitchFooterStatus(YacaPluginInfo plugin)
    {
        if (plugin is null)
        {
            return;
        }

        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";

        GlobalFooterStatusText.Text = IsGerman
            ? $"Plugin gewechselt auf: Yaca {plugin.Version} Punkt (Build: {build})"
            : $"Plugin switched to: Yaca {plugin.Version} Point (Build: {build})";

        GlobalFooterStatusText.Foreground =
            (Brush)FindResource("SuccessBrush");
        GlobalFooterStatusText.FontWeight = FontWeights.Bold;
        GlobalFooterStatusText.TextWrapping = TextWrapping.NoWrap;
        GlobalFooterStatusText.TextTrimming = TextTrimming.None;
        GlobalFooterStatusText.VerticalAlignment = VerticalAlignment.Center;
    }

    /// <summary>
    /// Zeigt einen Fehler im globalen Footerstatus an.
    /// </summary>
    private void ShowError(string message)
    {
        SetGlobalStatus(message);
    }
}
