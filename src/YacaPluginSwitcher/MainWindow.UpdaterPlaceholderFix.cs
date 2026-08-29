using System.Windows;
using System.Windows.Controls;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    // Kept only for compatibility with the existing dashboard wiring.
    // The updater will be integrated properly later; there is intentionally
    // no "Coming Soon" badge or placeholder UI.
    private void ShowComingSoon()
    {
        ShowError(IsGerman
            ? "Der YACA Updater ist noch nicht integriert."
            : "The YACA Updater is not integrated yet.");
    }

    private void RemoveUpdaterPlaceholderBadge()
    {
        foreach (var button in FindVisualButtons(PageHost).ToList())
        {
            if (button.Content is not Grid grid) continue;
            foreach (var child in grid.Children.OfType<Border>().ToList())
            {
                if (child.Child is TextBlock text &&
                    (text.Text.Contains("BALD", StringComparison.OrdinalIgnoreCase) ||
                     text.Text.Contains("COMING", StringComparison.OrdinalIgnoreCase)))
                {
                    grid.Children.Remove(child);
                }
            }
        }
    }
}
