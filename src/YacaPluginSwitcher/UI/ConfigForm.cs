using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher.UI;

public sealed class ConfigForm : Form
{
    private const int FormWidth = 840;
    private const int NormalHeight = 600;
    private const int ExpertHeight = 760;
    private const int ExpertContentMinHeight = 420;

    private readonly YacaService _service;
    private readonly CheckBox _expertSettings = new();
    private readonly CheckBox _multipleInstances = new();
    private readonly CheckBox _generalLogging = new();
    private readonly CheckBox _debugLogging = new();
    private readonly CheckBox _selectableBackups = new();
    private readonly ListBox _pathsList = new();
    private readonly Button _addPathButton = new();
    private readonly Button _removePathButton = new();
    private readonly Button _useSelectedPathButton = new();
    private readonly Button _autoDetectButton = new();
    private readonly NumericUpDown _maxBackups = new();
    private readonly CheckBox _automaticBackup = new();
    private readonly CheckBox _warnIfRunning = new();
    private readonly ComboBox _language = new();
    private readonly TextBox _activePath = new();
    private readonly Button _browseActivePath = new();
    private readonly Panel _expertPanel = new();
    private readonly TableLayoutPanel _expertContent = new();
    private bool _loadingSettings;

    public ConfigForm(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        var text = Texts;

        Text = text.ConfigTitle;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(FormWidth, NormalHeight);
        MinimumSize = new Size(760, NormalHeight);
        MaximumSize = new Size(FormWidth, ExpertHeight);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);

        BuildUi(text);
        LoadSettings();
    }

    private UiText Texts => Localization.Get(_service.Settings.Language);

    private void BuildUi(UiText text)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = text.ConfigTitle,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = Theme.Accent,
            BackColor = Theme.Background,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var languagePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        languagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        languagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        languagePanel.Controls.Add(new Label
        {
            Text = text.Language,
            Dock = DockStyle.Fill,
            ForeColor = Theme.SecondaryForeground,
            BackColor = Theme.Background,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        Theme.StyleComboBox(_language);
        _language.Items.Add(text.LanguageEnglish);
        _language.Items.Add(text.LanguageGerman);
        _language.Width = 190;
        languagePanel.Controls.Add(_language, 1, 0);
        root.Controls.Add(languagePanel, 0, 1);

        var standard = BuildStandardOptions(text);
        root.Controls.Add(standard, 0, 2);

        _expertSettings.Text = text.ExpertSettings;
        _expertSettings.AutoSize = true;
        _expertSettings.Dock = DockStyle.Fill;
        _expertSettings.Padding = new Padding(2, 4, 0, 0);
        _expertSettings.BackColor = Theme.Surface;
        _expertSettings.ForeColor = Theme.Foreground;
        _expertSettings.CheckedChanged += (_, _) => UpdateExpertUi();
        root.Controls.Add(_expertSettings, 0, 3);

        BuildExpertPanel(text);
        root.Controls.Add(_expertPanel, 0, 4);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(0, 5, 0, 0)
        };
        AddButton(buttons, text.Save, 120, SaveSettings);
        AddButton(buttons, text.Cancel, 120, CancelSettings);
        footer.Controls.Add(buttons, 1, 0);
        root.Controls.Add(footer, 0, 5);
    }

    private TableLayoutPanel BuildStandardOptions(UiText text)
    {
        var standard = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Padding = new Padding(12)
        };
        standard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        standard.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        standard.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        standard.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        standard.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        standard.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        standard.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _automaticBackup.Text = text.AutomaticBackup;
        _automaticBackup.AutoSize = true;
        _automaticBackup.BackColor = Theme.Surface;
        _automaticBackup.ForeColor = Theme.Foreground;
        standard.Controls.Add(_automaticBackup, 0, 0);
        standard.SetColumnSpan(_automaticBackup, 2);

        standard.Controls.Add(MakeLabel(text.MaxBackups), 0, 1);
        _maxBackups.Minimum = 1;
        _maxBackups.Maximum = 9;
        _maxBackups.Width = 100;
        _maxBackups.Anchor = AnchorStyles.Left;
        _maxBackups.BackColor = Theme.Control;
        _maxBackups.ForeColor = Theme.Foreground;
        standard.Controls.Add(_maxBackups, 1, 1);

        _warnIfRunning.Text = text.WarnIfTeamSpeakRunningOption;
        _warnIfRunning.AutoSize = true;
        _warnIfRunning.BackColor = Theme.Surface;
        _warnIfRunning.ForeColor = Theme.Foreground;
        standard.Controls.Add(_warnIfRunning, 0, 2);
        standard.SetColumnSpan(_warnIfRunning, 2);

        standard.Controls.Add(MakeLabel(text.ActiveTeamSpeakPath), 0, 3);
        standard.SetColumnSpan(standard.GetControlFromPosition(0, 3)!, 2);

        var activePathPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Surface,
            Margin = Padding.Empty
        };
        activePathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        activePathPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        _activePath.Dock = DockStyle.Fill;
        _activePath.BackColor = Theme.Control;
        _activePath.ForeColor = Theme.Foreground;
        _activePath.BorderStyle = BorderStyle.FixedSingle;
        _activePath.Margin = new Padding(0, 1, 5, 1);
        activePathPanel.Controls.Add(_activePath, 0, 0);
        ConfigureButton(_browseActivePath, text.Browse, 110, BrowseActivePath);
        _browseActivePath.Margin = new Padding(0, 0, 0, 0);
        activePathPanel.Controls.Add(_browseActivePath, 1, 0);
        standard.Controls.Add(activePathPanel, 0, 4);
        standard.SetColumnSpan(activePathPanel, 2);
        return standard;
    }

    private void BuildExpertPanel(UiText text)
    {
        _expertPanel.Dock = DockStyle.Fill;
        _expertPanel.AutoScroll = false;
        _expertPanel.BackColor = Theme.Surface;
        _expertPanel.ForeColor = Theme.Foreground;
        _expertPanel.Padding = Padding.Empty;

        _expertContent.Dock = DockStyle.Fill;
        _expertContent.AutoSize = false;
        _expertContent.ColumnCount = 1;
        _expertContent.RowCount = 4;
        _expertContent.BackColor = Theme.Surface;
        _expertContent.ForeColor = Theme.Foreground;
        _expertContent.Padding = new Padding(12);
        _expertContent.Margin = Padding.Empty;
        _expertContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _expertContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _expertContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        _expertContent.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        _expertPanel.Controls.Add(_expertContent);

        _multipleInstances.Text = text.MultipleTeamSpeakInstancesOption;
        _multipleInstances.AutoSize = true;
        _multipleInstances.Dock = DockStyle.Fill;
        _multipleInstances.Padding = Padding.Empty;
        _multipleInstances.Margin = Padding.Empty;
        _multipleInstances.BackColor = Theme.Surface;
        _multipleInstances.ForeColor = Theme.Foreground;
        _multipleInstances.CheckedChanged += (_, _) => UpdateMultipleInstanceUi();
        _expertContent.Controls.Add(_multipleInstances, 0, 0);

        var paths = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty
        };
        paths.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        paths.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        Theme.StyleListBox(_pathsList);
        _pathsList.Dock = DockStyle.Fill;
        _pathsList.IntegralHeight = false;
        _pathsList.Margin = Padding.Empty;
        paths.Controls.Add(_pathsList, 0, 0);

        var pathButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 0)
        };
        ConfigureButton(_addPathButton, text.AddPath, 120, AddPath);
        ConfigureButton(_removePathButton, text.RemovePath, 100, RemovePath);
        ConfigureButton(_useSelectedPathButton, text.UseSelectedPath, 125, UseSelectedPath);
        ConfigureButton(_autoDetectButton, text.AutoDetect, 130, AutoDetect);
        pathButtons.Controls.AddRange([_addPathButton, _removePathButton, _useSelectedPathButton, _autoDetectButton]);
        paths.Controls.Add(pathButtons, 0, 1);
        _expertContent.Controls.Add(paths, 0, 1);

        var logging = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty
        };
        _generalLogging.Text = text.GeneralLogging;
        _generalLogging.AutoSize = true;
        _generalLogging.Dock = DockStyle.Fill;
        _generalLogging.Padding = Padding.Empty;
        _generalLogging.Margin = Padding.Empty;
        _generalLogging.BackColor = Theme.Surface;
        _generalLogging.ForeColor = Theme.Foreground;
        _debugLogging.Text = text.DebugLogging;
        _debugLogging.AutoSize = true;
        _debugLogging.Dock = DockStyle.Fill;
        _debugLogging.Padding = Padding.Empty;
        _debugLogging.Margin = Padding.Empty;
        _debugLogging.BackColor = Theme.Surface;
        _debugLogging.ForeColor = Theme.Foreground;
        logging.Controls.Add(_generalLogging, 0, 0);
        logging.Controls.Add(_debugLogging, 0, 1);
        _expertContent.Controls.Add(logging, 0, 2);

        _selectableBackups.Text = text.SelectableBackups;
        _selectableBackups.AutoSize = true;
        _selectableBackups.Dock = DockStyle.Fill;
        _selectableBackups.Padding = Padding.Empty;
        _selectableBackups.Margin = Padding.Empty;
        _selectableBackups.BackColor = Theme.Surface;
        _selectableBackups.ForeColor = Theme.Foreground;
        _expertContent.Controls.Add(_selectableBackups, 0, 3);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        ForeColor = Theme.SecondaryForeground,
        BackColor = Theme.Surface,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void LoadSettings()
    {
        var settings = _service.Settings;
        _loadingSettings = true;
        try
        {
            _expertSettings.Checked = settings.ExpertSettings || settings.UseMultipleTeamSpeakInstances || settings.DebugLogging;
            _multipleInstances.Checked = settings.UseMultipleTeamSpeakInstances;

            _pathsList.Items.Clear();
            foreach (var path in settings.TeamSpeakPluginDirectories)
                _pathsList.Items.Add(path);

            var detected = YacaService.GetDefaultTeamSpeakPluginDirectory();
            var active = settings.UseMultipleTeamSpeakInstances && !string.IsNullOrWhiteSpace(settings.TeamSpeakPluginDirectory)
                ? settings.TeamSpeakPluginDirectory
                : !settings.UseMultipleTeamSpeakInstances && settings.UseCustomTeamSpeakPluginDirectory && !string.IsNullOrWhiteSpace(settings.TeamSpeakPluginDirectory)
                    ? settings.TeamSpeakPluginDirectory
                    : detected;
            _activePath.Text = active;

            if (_pathsList.Items.Count == 0)
                _pathsList.Items.Add(active);

            var index = FindPathIndex(active);
            _pathsList.SelectedIndex = index >= 0 ? index : 0;

            _maxBackups.Value = Math.Clamp(settings.MaxBackups, 1, 9);
            _automaticBackup.Checked = settings.AutomaticBackup;
            _warnIfRunning.Checked = settings.WarnIfTeamSpeakRunning;
            _generalLogging.Checked = settings.GeneralLogging;
            _debugLogging.Checked = settings.DebugLogging;
            _selectableBackups.Checked = settings.SelectableBackupsForDeletion;
            _language.SelectedIndex = string.Equals(settings.Language, Localization.German, StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
        finally
        {
            _loadingSettings = false;
            UpdateExpertUi();
        }
    }

    private void UpdateExpertUi()
    {
        var enabled = _expertSettings.Checked;
        _expertPanel.Visible = enabled;
        _multipleInstances.Enabled = enabled;
        _pathsList.Enabled = enabled && _multipleInstances.Checked;
        _addPathButton.Enabled = enabled && _multipleInstances.Checked;
        _removePathButton.Enabled = enabled && _multipleInstances.Checked;
        _useSelectedPathButton.Enabled = enabled && _multipleInstances.Checked;
        _autoDetectButton.Enabled = enabled && _multipleInstances.Checked;
        _generalLogging.Enabled = enabled;
        _debugLogging.Enabled = enabled;
        _selectableBackups.Enabled = enabled;

        var targetHeight = enabled ? ExpertHeight : NormalHeight;
        var maximumHeight = Screen.FromControl(this).WorkingArea.Height - 40;
        var minimumExpertHeight = Math.Min(maximumHeight, Math.Max(NormalHeight, ExpertContentMinHeight + 180));
        var desiredHeight = enabled ? Math.Max(minimumExpertHeight, targetHeight) : NormalHeight;
        if (Height != desiredHeight)
            Height = Math.Min(desiredHeight, maximumHeight);

        UpdateMultipleInstanceUi();
    }

    private void UpdateMultipleInstanceUi()
    {
        var enabled = _expertSettings.Checked && _multipleInstances.Checked;
        _pathsList.Enabled = enabled;
        _addPathButton.Enabled = enabled;
        _removePathButton.Enabled = enabled;
        _useSelectedPathButton.Enabled = enabled;
        _autoDetectButton.Enabled = enabled;
        _activePath.ReadOnly = enabled;
        _browseActivePath.Enabled = !enabled;

        if (!_loadingSettings && _expertSettings.Checked && !enabled)
            _activePath.Text = YacaService.GetDefaultTeamSpeakPluginDirectory();

        if (enabled && _pathsList.Items.Count == 0 && !string.IsNullOrWhiteSpace(_activePath.Text))
        {
            _pathsList.Items.Add(_activePath.Text);
            _pathsList.SelectedIndex = 0;
        }
    }

    private void AddPath()
    {
        if (!_expertSettings.Checked || !_multipleInstances.Checked)
            return;
        using var dialog = new FolderBrowserDialog
        {
            Description = Texts.TeamSpeakPluginPaths,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = _activePath.Text
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        var path = Path.GetFullPath(dialog.SelectedPath);
        if (FindPathIndex(path) >= 0)
        {
            MessageBox.Show(this, Texts.PathAlreadyExists, Texts.ConfigTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _pathsList.Items.Add(path);
        _pathsList.SelectedIndex = _pathsList.Items.Count - 1;
        _activePath.Text = path;
    }

    private void RemovePath()
    {
        if (!_multipleInstances.Checked || _pathsList.SelectedIndex < 0)
            return;
        var removedPath = _pathsList.SelectedItem as string;
        _pathsList.Items.RemoveAt(_pathsList.SelectedIndex);
        if (_pathsList.Items.Count > 0)
        {
            _pathsList.SelectedIndex = 0;
            if (string.Equals(_activePath.Text, removedPath, StringComparison.OrdinalIgnoreCase))
                _activePath.Text = _pathsList.SelectedItem as string ?? string.Empty;
        }
        else
            _activePath.Clear();
    }

    private void UseSelectedPath()
    {
        if (!_multipleInstances.Checked || _pathsList.SelectedItem is not string path)
            return;
        _activePath.Text = path;
    }

    private void AutoDetect()
    {
        if (!_multipleInstances.Checked)
            return;
        foreach (var path in YacaService.GetTeamSpeakPluginDirectoryCandidates())
        {
            if (FindPathIndex(path) < 0)
                _pathsList.Items.Add(path);
        }
        var detected = YacaService.GetDefaultTeamSpeakPluginDirectory();
        var index = FindPathIndex(detected);
        if (index >= 0)
            _pathsList.SelectedIndex = index;
        _activePath.Text = detected;
    }

    private void BrowseActivePath()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Texts.ActiveTeamSpeakPath,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = GetBrowseStartPath(_activePath.Text)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        _activePath.Text = NormalizeTeamSpeakPluginDirectory(dialog.SelectedPath);
    }

    private void CancelSettings()
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void SaveSettings()
    {
        try
        {
            var active = _activePath.Text.Trim();
            if (string.IsNullOrWhiteSpace(active))
            {
                MessageBox.Show(this, Texts.PathMustBeValid, Texts.ConfigTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var fullActive = NormalizeTeamSpeakPluginDirectory(active);
            Directory.CreateDirectory(fullActive);

            var targetYacaFile = Path.Combine(fullActive, YacaService.TargetFileName);
            if (!File.Exists(targetYacaFile))
            {
                var result = MessageBox.Show(
                    this,
                    Texts.TeamSpeakPathWarningMessage,
                    Texts.TeamSpeakPathWarningTitle,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;
            }

            _service.Settings.ExpertSettings = _expertSettings.Checked;
            _service.Settings.UseMultipleTeamSpeakInstances = _expertSettings.Checked && _multipleInstances.Checked;
            _service.Settings.GeneralLogging = _expertSettings.Checked ? _generalLogging.Checked : _service.Settings.GeneralLogging;
            _service.Settings.DebugLogging = _expertSettings.Checked && _debugLogging.Checked;
            // Keep expert-only values persisted even while the expert section is hidden.
            // The setting only affects the Backup UI when the expert option is enabled.
            _service.Settings.SelectableBackupsForDeletion = _selectableBackups.Checked;

            if (_service.Settings.UseMultipleTeamSpeakInstances)
            {
                _service.Settings.TeamSpeakPluginDirectory = fullActive;
                _service.Settings.UseCustomTeamSpeakPluginDirectory = false;
                _service.Settings.TeamSpeakPluginDirectories = _pathsList.Items.Cast<string>()
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .Select(Path.GetFullPath)
                    .Append(fullActive)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                var detected = YacaService.GetDefaultTeamSpeakPluginDirectory();
                var isDefault = string.Equals(fullActive, detected, StringComparison.OrdinalIgnoreCase);
                _service.Settings.UseCustomTeamSpeakPluginDirectory = !isDefault;
                _service.Settings.TeamSpeakPluginDirectory = isDefault ? null : fullActive;
            }

            _service.Settings.MaxBackups = (int)_maxBackups.Value;
            _service.Settings.AutomaticBackup = _automaticBackup.Checked;
            _service.Settings.WarnIfTeamSpeakRunning = _warnIfRunning.Checked;
            _service.Settings.Language = _language.SelectedIndex == 1 ? Localization.German : Localization.English;
            _service.Settings.Save();
            _service.Logger.Configure(_service.Settings.GeneralLogging, _service.Settings.DebugLogging);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is YacaOperationException or ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _service.Logger.Error($"Configuration save failed: {ex}");
            MessageBox.Show(this, Localization.GetErrorMessage(ex, Texts, Texts.PathMustBeValid), Texts.ConfigTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string GetBrowseStartPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return NormalizeTeamSpeakPluginDirectory(path);
    }

    private static string NormalizeTeamSpeakPluginDirectory(string path)
    {
        var fullPath = Path.GetFullPath(path.Trim());
        var directoryName = new DirectoryInfo(fullPath).Name;

        if (directoryName.Equals("TS3Client", StringComparison.OrdinalIgnoreCase) ||
            directoryName.Equals("TeamSpeak 3 Client", StringComparison.OrdinalIgnoreCase))
        {
            // The configuration always stores the actual TS3 plugin directory,
            // never the TS3 client root. The plugins directory may not exist yet;
            // SaveSettings() will create it when the user explicitly confirms.
            return Path.Combine(fullPath, "plugins");
        }

        return fullPath;
    }

    private int FindPathIndex(string path)
    {
        for (var i = 0; i < _pathsList.Items.Count; i++)
        {
            if (_pathsList.Items[i] is string candidate && string.Equals(candidate, path, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static void ConfigureButton(Button button, string text, int width, Action action)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 34;
        button.Margin = new Padding(3, 1, 3, 1);
        Theme.StyleButton(button);
        button.Click += (_, _) => action();
    }

    private static void AddButton(FlowLayoutPanel host, string text, int width, Action action)
    {
        var button = new Button { Text = text, Width = width, Height = 34, Margin = new Padding(3, 1, 3, 1) };
        Theme.StyleButton(button);
        button.Click += (_, _) => action();
        host.Controls.Add(button);
    }
}
