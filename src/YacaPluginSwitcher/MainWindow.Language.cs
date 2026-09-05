using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    internal void ChangeLanguage(string language)
    {
        var normalized = Localization.Normalize(language);
        if (string.Equals(
                Localization.Normalize(_service.Settings.Language),
                normalized,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _service.Settings.Language = normalized;
        _service.Settings.Save();

        BuildNavigation();
        LoadLanguageSelector();
        ShowCurrentPageAfterLanguageChange();

        SetGlobalStatus(normalized == Localization.German
            ? "Sprache zu Deutsch gewechselt."
            : "Language changed to English.");
    }

    internal void RefreshNavigationLanguage()
    {
        BuildNavigation();
        LoadLanguageSelector();
        SetActiveNav(_activePage);
    }

    private void LanguageCombo_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (LanguageCombo.IsDropDownOpen)
            return;

        LanguageCombo.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() => LanguageCombo.IsDropDownOpen = true));
    }

    private void LoadLanguageSelector()
    {
        LanguageCombo.Items.Clear();
        LanguageCombo.Items.Add(Texts.LanguageGerman);
        LanguageCombo.Items.Add(Texts.LanguageEnglish);
        LanguageCombo.SelectedIndex = IsGerman ? 0 : 1;
    }

    private void LanguageCombo_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedIndex < 0)
            return;

        var language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;
        ChangeLanguage(language);
    }
}
