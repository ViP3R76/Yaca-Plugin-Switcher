using System.Windows.Input;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Aktualisiert Navigation und Sprachauswahl nach einer Sprachänderung.
    /// </summary>
    internal void RefreshNavigationLanguage()
    {
        BuildNavigation();
        LoadLanguageSelector();
        SetActiveNav(_activePage);
    }

    /// <summary>
    /// Öffnet die Sprachauswahl per Mausklick, falls sie noch geschlossen ist.
    /// </summary>
    private void LanguageCombo_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (LanguageCombo.IsDropDownOpen)
        {
            return;
        }

        LanguageCombo.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() => LanguageCombo.IsDropDownOpen = true));
    }

    /// <summary>
    /// Befüllt die Sprachauswahl mit den verfügbaren Sprachen.
    /// </summary>
    private void LoadLanguageSelector()
    {
        LanguageCombo.Items.Clear();
        LanguageCombo.Items.Add(Texts.LanguageGerman);
        LanguageCombo.Items.Add(Texts.LanguageEnglish);
        LanguageCombo.SelectedIndex = IsGerman ? 0 : 1;
    }

    /// <summary>
    /// Speichert eine geänderte Sprache und baut die aktuelle Ansicht neu auf.
    /// </summary>
    private void LanguageCombo_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedIndex < 0)
        {
            return;
        }

        var language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;

        if (string.Equals(
                Localization.Normalize(_service.Settings.Language),
                language,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _service.Settings.Language = language;
        _service.Settings.Save();

        BuildNavigation();
        LoadLanguageSelector();
        ShowCurrentPageAfterLanguageChange();
    }
}
