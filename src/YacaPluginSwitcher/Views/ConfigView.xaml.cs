using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

/// <summary>
/// Konfigurationsansicht des YACA Plugin Switchers.
/// Die Ansicht lädt und speichert ausschließlich Einstellungen; fachliche
/// Update- und Installationslogik verbleibt in den Core-Diensten.
/// </summary>
public partial class ConfigView : UserControl
{
    private readonly YacaService _service;
    private readonly MainWindow _owner;
    private bool _loading;
    private bool _useCustomPath;
    private bool _hasPendingChanges;

    private UiText Texts => Localization.Get(_service.Settings.Language);

    public ConfigView(YacaService service, MainWindow owner)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));

        InitializeComponent();
        RegisterPendingChangeHandlers();
        LoadSettings();
    }

    private string SettingsText(string key) => SettingsLocalization.Get(_service.Settings.Language, key);

    private void RegisterPendingChangeHandlers()
    {
        foreach (var checkBox in new[]
                 {
                     AutomaticBackup,
                     WarnRunning,
                     KeepYacaPluginDownloads,
                     DownloadAllWithoutPrompt,
                     GeneralLogging,
                     DebugLogging,
                     SelectableBackups
                 })
        {
            checkBox.Checked += SettingChanged;
            checkBox.Unchecked += SettingChanged;
        }

        MaxBackups.SelectionChanged += MaxBackups_SelectionChanged;
        ActivePath.TextChanged += ActivePath_TextChanged;
    }

    private void LoadSettings()
    {
        _loading = true;
        _hasPendingChanges = false;

        try
        {
            TitleText.Text = SettingsText("Configuration");
            PendingChangesText.Text = SettingsText("PendingChanges");
            GeneralHeader.Text = SettingsText("General");
            LanguageLabel.Text = SettingsText("Language");
            YacaDownloaderHeader.Text = SettingsText("YacaDownloader");
            TeamSpeakHeader.Text = SettingsText("TeamSpeak");
            ActiveTeamSpeakPathLabel.Text = SettingsText("ActiveTeamSpeakPath");
            ActivePath.ToolTip = SettingsText("TeamSpeakPathTooltip");
            BrowseButton.Content = SettingsText("Browse");
            Expert.Content = SettingsText("ExpertSettings");
            MultipleInstances.Content = SettingsText("MultipleInstances");
            TeamSpeakInstancesHeader.Text = SettingsText("TeamSpeakInstances");
            AvailableTeamSpeakPathsLabel.Text = SettingsText("AvailableTeamSpeakPaths");
            AddPathButton.Content = SettingsText("AddPath");
            RemovePathButton.Content = SettingsText("Remove");
            UsePathButton.Content = SettingsText("UseSelectedPath");
            AutoDetectButton.Content = SettingsText("AutoDetect");
            LoggingBackupsHeader.Text = SettingsText("LoggingBackups");
            LogDirectoryLabel.Text = SettingsText("LogDirectory");
            OpenLogButton.Content = SettingsText("Open");
            ApplicationDirectoriesHeader.Text = SettingsText("ApplicationDirectories");
            BackupsDirectoryLabel.Text = SettingsText("Backups");
            PluginsDirectoryLabel.Text = SettingsText("Plugins");
            ApplicationDirectoryLabel.Text = SettingsText("ApplicationDirectory");
            OpenBackupButton.Content = SettingsText("Open");
            OpenPluginButton.Content = SettingsText("Open");
            OpenAppButton.Content = SettingsText("Open");
            SaveButton.Content = SettingsText("Save");
            CancelButton.Content = SettingsText("Cancel");
            MaxBackupsLabel.Text = SettingsText("MaximumBackups");
            AutomaticBackup.Content = SettingsText("AutomaticBackup");
            WarnRunning.Content = SettingsText("WarnRunning");
            KeepYacaPluginDownloads.Content = SettingsText("KeepDownloads");
            DownloadAllWithoutPrompt.Content = SettingsText("DownloadAll");
            GeneralLogging.Content = Texts.GeneralLogging;
            DebugLogging.Content = Texts.DebugLogging;
            SelectableBackups.Content = SettingsText("SelectableBackups");

            var isGerman = Localization.Normalize(_service.Settings.Language) == Localization.German;
            LanguageCombo.Items.Clear();
            LanguageCombo.Items.Add(Localization.Get(Localization.German).LanguageGerman);
            LanguageCombo.Items.Add(Localization.Get(Localization.English).LanguageEnglish);
            LanguageCombo.SelectedIndex = isGerman ? 0 : 1;

            MaxBackups.Items.Clear();
            for (var value = 1; value <= 9; value++)
                MaxBackups.Items.Add(value);

            AutomaticBackup.IsChecked = _service.Settings.AutomaticBackup;
            WarnRunning.IsChecked = _service.Settings.WarnIfTeamSpeakRunning;
            KeepYacaPluginDownloads.IsChecked = _service.Settings.KeepYacaPluginDownloads;
            DownloadAllWithoutPrompt.IsChecked = _service.Settings.DownloadAllPluginsWithoutPrompt;
            Expert.IsChecked = _service.Settings.ExpertSettings;
            MaxBackups.SelectedItem = Math.Clamp(_service.Settings.MaxBackups, 1, 9);
            _useCustomPath = _service.Settings.UseCustomTeamSpeakPluginDirectory;
            ActivePath.Text = _service.Settings.TeamSpeakPluginDirectory
                ?? YacaService.GetDefaultTeamSpeakPluginDirectory();
            MultipleInstances.IsChecked = _service.Settings.UseMultipleTeamSpeakInstances;
            GeneralLogging.IsChecked = _service.Settings.GeneralLogging;
            DebugLogging.IsChecked = _service.Settings.DebugLogging;
            SelectableBackups.IsChecked = _service.Settings.SelectableBackupsForDeletion;
            PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();

            LogDirectory.Text = _service.Paths.LogDirectory;
            BackupDirectory.Text = _service.Paths.BackupDirectory;
            PluginDirectory.Text = _service.Paths.PluginDirectory;
            AppDirectory.Text = _service.Paths.BaseDirectory;

            UpdateExpert();
            UpdatePendingChangesIndicator();
        }
        finally
        {
            _loading = false;
        }
    }

    private void UpdateExpert()
    {
        var expertEnabled = Expert.IsChecked == true;
        ExpertPanel.Visibility = expertEnabled ? Visibility.Visible : Visibility.Collapsed;
        TeamSpeakInstancesPanel.Visibility = expertEnabled && MultipleInstances.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void MarkPendingChange()
    {
        if (_loading)
            return;

        _hasPendingChanges = true;
        UpdatePendingChangesIndicator();
    }

    private void UpdatePendingChangesIndicator()
    {
        if (PendingChangesText is null)
            return;

        PendingChangesText.Text = SettingsText("PendingChanges");
        PendingChangesText.Visibility = _hasPendingChanges
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SettingChanged(object sender, RoutedEventArgs e) => MarkPendingChange();

    private void MaxBackups_SelectionChanged(object sender, SelectionChangedEventArgs e) => MarkPendingChange();

    private void ActivePath_TextChanged(object sender, TextChangedEventArgs e) => MarkPendingChange();

    private void Expert_Changed(object sender, RoutedEventArgs e)
    {
        UpdateExpert();
        MarkPendingChange();
    }

    private void MultipleInstances_Changed(object sender, RoutedEventArgs e)
    {
        UpdateExpert();
        MarkPendingChange();
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageCombo.SelectedIndex < 0)
            return;

        var language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;
        _owner.ChangeLanguage(language);
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
        var dialog = new OpenFolderDialog { Title = Texts.AddPath };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _service.Settings.AddTeamSpeakPluginDirectory(dialog.FolderName);
            PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();
            MarkPendingChange();
        }
    }

    private void RemovePath_Click(object sender, RoutedEventArgs e)
    {
        if (PathsList.SelectedItem is not string path)
            return;

        _service.Settings.RemoveTeamSpeakPluginDirectory(path);
        PathsList.ItemsSource = _service.Settings.TeamSpeakPluginDirectories.ToList();
        MarkPendingChange();
    }

    private void UsePath_Click(object sender, RoutedEventArgs e)
    {
        if (PathsList.SelectedItem is string path)
        {
            ActivePath.Text = path;
            _useCustomPath = true;
            MarkPendingChange();
        }
    }

    private void AutoDetect_Click(object sender, RoutedEventArgs e)
    {
        ActivePath.Text = YacaService.GetDefaultTeamSpeakPluginPluginDirectory();
        _useCustomPath = false;
        MarkPendingChange();
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
        {
            ActivePath.Text = dialog.FolderName;
            _useCustomPath = true;
            MarkPendingChange();
        }
    }

    private static void OpenDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        Process.Start(new ProcessStartInfo
        {
            FileName = directory,
            UseShellExecute = true
        });
    }

    private void OpenLogDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory(_service.Paths.LogDirectory);
    private void OpenBackupDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory(_service.Paths.BackupDirectory);
    private void OpenPluginDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory(_service.Paths.PluginDirectory);
    private void OpenAppDirectory_Click(object sender, RoutedEventArgs e) => OpenDirectory(_service.Paths.BaseDirectory);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (MaxBackups.SelectedItem is not int max || max is < 1 or > 9)
        {
            MessageBox.Show(Texts.ErrorInvalidArgument, Texts.ConfigTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var path = ActivePath.Text.Trim();
        if (_useCustomPath && !Directory.Exists(path))
        {
            MessageBox.Show(Texts.PathMustBeValid, Texts.ConfigTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _service.Settings.AutomaticBackup = AutomaticBackup.IsChecked == true;
        _service.Settings.WarnIfTeamSpeakRunning = WarnRunning.IsChecked == true;
        _service.Settings.KeepYacaPluginDownloads = KeepYacaPluginDownloads.IsChecked == true;
        _service.Settings.DownloadAllPluginsWithoutPrompt = DownloadAllWithoutPrompt.IsChecked == true;
        _service.Settings.ExpertSettings = Expert.IsChecked == true;
        _service.Settings.MaxBackups = max;
        _service.Settings.UseCustomTeamSpeakPluginDirectory = _useCustomPath;
        _service.Settings.TeamSpeakPluginDirectory = _useCustomPath ? path : null;
        _service.Settings.UseMultipleTeamSpeakInstances = MultipleInstances.IsChecked == true;
        _service.Settings.GeneralLogging = GeneralLogging.IsChecked == true;
        _service.Settings.DebugLogging = DebugLogging.IsChecked == true;
        _service.Settings.SelectableBackupsForDeletion = SelectableBackups.IsChecked == true;
        _service.Logger.Configure(_service.Settings.GeneralLogging, _service.Settings.DebugLogging);
        _service.Settings.Save();
        _hasPendingChanges = false;
        UpdatePendingChangesIndicator();

        _owner.ReturnHome();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _owner.ReturnHome();
}
