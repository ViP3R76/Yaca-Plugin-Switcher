using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    internal void RefreshNavigationLanguage()
    {
        BuildNavigation();
        LoadLanguageSelector();
        SetActiveNav(_activePage);
    }

    private void LanguageCombo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (LanguageCombo.IsDropDownOpen)
            return;

        LanguageCombo.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() => LanguageCombo.IsDropDownOpen = true));
    }
}
