using System.Windows.Controls;
using System.Windows;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private bool _languageSelectionUpdating;

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_languageSelectionUpdating || LanguageCombo.SelectedIndex < 0)
            return;

        var language = LanguageCombo.SelectedIndex == 0 ? Localization.German : Localization.English;
        if (string.Equals(_service.Settings.Language, language, StringComparison.OrdinalIgnoreCase))
            return;

        _languageSelectionUpdating = true;
        try
        {
            _service.Settings.Language = language;
            _service.Settings.Save();
            BuildNavigation();
            LoadLanguageSelector();
            ShowHome();
        }
        finally
        {
            _languageSelectionUpdating = false;
        }
    }
}
