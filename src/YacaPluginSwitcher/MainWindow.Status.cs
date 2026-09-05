using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private DispatcherTimer? _footerStatusTimer;

    private void SetGlobalStatus(string message, bool success = false)
    {
        GlobalFooterStatusText.Text = message;
        GlobalFooterStatusText.Foreground = (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush");
        GlobalFooterStatusText.FontWeight = success ? FontWeights.Bold : FontWeights.Normal;
        GlobalFooterStatusText.TextWrapping = TextWrapping.NoWrap;
        GlobalFooterStatusText.TextTrimming = TextTrimming.None;
        GlobalFooterStatusText.TextAlignment = TextAlignment.Center;
        GlobalFooterStatusText.VerticalAlignment = VerticalAlignment.Center;

        if (success)
        {
            StopFooterStatusTimer();
            return;
        }

        StartFooterStatusTimer();
    }

    private void SetGlobalWarningStatus(string message)
    {
        StopFooterStatusTimer();
        GlobalFooterStatusText.Text = message;
        GlobalFooterStatusText.Foreground = (Brush)FindResource("GoldBrush");
        GlobalFooterStatusText.FontWeight = FontWeights.Bold;
        GlobalFooterStatusText.TextWrapping = TextWrapping.NoWrap;
        GlobalFooterStatusText.TextTrimming = TextTrimming.None;
        GlobalFooterStatusText.TextAlignment = TextAlignment.Center;
        GlobalFooterStatusText.VerticalAlignment = VerticalAlignment.Center;
    }

    private void StartFooterStatusTimer()
    {
        _footerStatusTimer ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _footerStatusTimer.Stop();
        _footerStatusTimer.Tick -= FooterStatusTimer_Tick;
        _footerStatusTimer.Tick += FooterStatusTimer_Tick;
        _footerStatusTimer.Start();
    }

    private void StopFooterStatusTimer()
    {
        _footerStatusTimer?.Stop();
    }

    private void FooterStatusTimer_Tick(object? sender, EventArgs e)
    {
        StopFooterStatusTimer();
        GlobalFooterStatusText.Text = string.Empty;
        GlobalFooterStatusText.Foreground = (Brush)FindResource("ForegroundBrush");
        GlobalFooterStatusText.FontWeight = FontWeights.Normal;
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
