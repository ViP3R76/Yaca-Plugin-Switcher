using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

/// <summary>
/// Konfigurationsansicht des YACA Plugin Switchers.
/// Die Ansicht lädt und speichert ausschließlich die Einstellungen; fachliche
/// Update- und Installationslogik verbleibt in den Core-Diensten.
/// </summary>
public partial class ConfigView : UserControl
{
    private readonly YacaService _service;
    private readonly MainWindow _owner;
    private bool _loading;

    private UiText Texts => Localization.Get(_service.Settings.Language);

    public ConfigView(YacaService service, MainWindow owner)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        InitializeComponent();
        LoadSettings();
    }

    /// <summary>
    /// Überträgt die aktuell gespeicherten Werte in die Steuerelemente.
    /// </summary>
    private void LoadSettings()
    {
        _loading = true;

        try
        {
            var text = Texts;
            var isGerman = Localization.Normalize(_service.Settings.Language) == Localization.German;

            TitleText.Text = isGerman ? "KONFIGURATION" : "CONFIGURATION";
            AutomaticBackup.Content = text.AutomaticBackup;
            WarnRunning.Content = text.WarnIfTeamSpeakRunningOption;
            KeepYacaPluginDownloads.Content = isGerman
                ? "Yaca Plugin Downloads behalten"
                : "Keep Yaca plugin downloads";
            DownloadAllWithoutPrompt.Content = isGerman
                ? "Alle Plugins direkt downloaden, ohne Nachfrage"
                : "Download all plugins directly without prompting";
            Expert.Content = text.ExpertSettings;
            MultipleInstances.Content = text.MultipleTeamSpeakInstancesOption;
            GeneralLogging.Content = text.GeneralLogging;
            DebugLogging.Content = text.DebugLogging;
            SelectableBackups.Content = text.SelectableBackups;

            LanguageCombo.Items.Clear();
            LanguageCombo.Items.Add(text.LanguageGerman);
            LanguageCombo.Items.Add(text.LanguageEnglish);
            LanguageCombo.SelectedIndex = isGerman ? 0 : 1;

            AutomaticBackup.IsChecked = _service.Settings.AutomaticBackup;
            WarnRunning.IsChecked = _service.Settings.WarnIfTeamSpeakRunning;
            KeepYacaPluginDownloads.IsChecked = _service.Settings.KeepYacaPluginDownloads;
            DownloadAllWithoutPrompt.IsChecked = _service.Settings.DownloadAllPluginsWithoutPrompt;
            Expert.IsChecked = _service.Settings.ExpertSettings;
            MaxBackups.Text = _service.Settings.MaxBackups.ToString(CultureInfo.InvariantCulture);
            ActivePath.Text = _service.Settings.TeamSpeakPluginDirectory
                ?? YacaService.GetDefaultTeamSpeakPluginDirectory();
            MultipleInstances.IsChecked = _service.Settings.UseMultipleTeamSpeakInstances;
            GeneralLogging.IsChecked = _service.Settings.GeneralLogging;
            DebugLogging.IsChecked = _service.Settings.DebugLogging;
            SelectableBackups.IsChecked = _service.Settings.SelectableBackupsForDeletion;
            PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();

            UpdateExpert();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// Blendet den Bereich für Experteneinstellungen ein oder aus.
    /// </summary>
    private void UpdateExpert()
    {
        ExpertPanel.Visibility = Expert.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void Expert_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loading)
            UpdateExpert();
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageCombo.SelectedIndex < 0)
            return;

        _service.Settings.Language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;

        _owner.RefreshNavigationLanguage();
        LoadSettings();
    }

    private void LanguageCombo_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (LanguageCombo.IsDropDownOpen)
            return;

        LanguageCombo.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Input,
            new Action(() => LanguageCombo.IsDropDownOpen = true));
    }

    private void AddPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = Texts.AddPath
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _service.Settings.AddTeamSpeakPluginDirectory(dialog.FolderName);
            PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();
        }
    }

    private void RemovePath_Click(object sender, RoutedEventArgs e)
    {
        if (PathsList.SelectedItem is not string path)
            return;

        _service.Settings.RemoveTeamSpeakPluginDirectory(path);
        PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();
    }

    private void UsePath_Click(object sender, RoutedEventArgs e)
    {
        if (PathsList.SelectedItem is string path)
            ActivePath.Text = path;
    }

    private void AutoDetect_Click(object sender, RoutedEventArgs e)
    {
        ActivePath.Text = YacaService.GetDefaultTeamSpeakPluginDirectory();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = Texts.Browse,
            InitialDirectory = Directory.Exists(ActivePath.Text)
                ? ActivePath.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            ActivePath.Text = dialog.FolderName;
    }

    /// <summary>
    /// Validiert und speichert die Einstellungen atomar über AppSettings.Save().
    /// </summary>
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(
                MaxBackups.Text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var max)
            || max < 1
            || max > 9)
        {
            MessageBox.Show(
                Texts.ErrorInvalidArgument,
                Texts.ConfigTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var path = ActivePath.Text.Trim();
        if (!Directory.Exists(path))
        {
            MessageBox.Show(
                Texts.PathMustBeValid,
                Texts.ConfigTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        _service.Settings.AutomaticBackup = AutomaticBackup.IsChecked == true;
        _service.Settings.WarnIfTeamSpeakRunning = WarnRunning.IsChecked == true;
        _service.Settings.KeepYacaPluginDownloads = KeepYacaPluginDownloads.IsChecked == true;
        _service.Settings.DownloadAllPluginsWithoutPrompt = DownloadAllWithoutPrompt.IsChecked == true;
        _service.Settings.ExpertSettings = Expert.IsChecked == true;
        _service.Settings.MaxBackups = max;
        _service.Settings.TeamSpeakPluginDirectory = path;
        _service.Settings.UseMultipleTeamSpeakInstances = MultipleInstances.IsChecked == true;
        _service.Settings.GeneralLogging = GeneralLogging.IsChecked == true;
        _service.Settings.DebugLogging = DebugLogging.IsChecked == true;
        _service.Settings.SelectableBackupsForDeletion = SelectableBackups.IsChecked == true;
        _service.Logger.Configure(_service.Settings.GeneralLogging, _service.Settings.DebugLogging);
        _service.Settings.Save();

        _owner.ReturnHome();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _owner.ReturnHome();
    }
}
