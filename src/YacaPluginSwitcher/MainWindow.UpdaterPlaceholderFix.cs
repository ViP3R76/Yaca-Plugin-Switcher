using System.Windows;
using System.Windows.Controls;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    static MainWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(Border),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler(HideUpdaterPlaceholderBadge));
    }

    // Compatibility target for the existing dashboard wiring. The updater itself
    // will be integrated later; no placeholder badge is displayed.
    private void ShowComingSoon()
    {
        ShowError(IsGerman
            ? "Der YACA Updater ist noch nicht integriert."
            : "The YACA Updater is not integrated yet.");
    }

    private static void HideUpdaterPlaceholderBadge(object sender, RoutedEventArgs e)
    {
        if (sender is not Border border || border.Child is not TextBlock text) return;
        if (!text.Text.Contains("BALD", StringComparison.OrdinalIgnoreCase) &&
            !text.Text.Contains("COMING", StringComparison.OrdinalIgnoreCase)) return;

        border.Visibility = Visibility.Collapsed;
        e.Handled = true;
    }
}
