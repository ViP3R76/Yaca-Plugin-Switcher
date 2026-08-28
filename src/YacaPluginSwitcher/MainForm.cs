using System.Diagnostics;
using System.Globalization;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;
using YacaPluginSwitcher.UI;

namespace YacaPluginSwitcher;

public sealed class MainForm : Form
{
    private readonly YacaService _service;
    private Label _current = new();
    private Label _status = new();
    private Label _paths = new();
    private Panel _plugins = new();
    private TableLayoutPanel? _pluginRows;
    private TableLayoutPanel? _root;
    private Button? _closeTeamSpeakButton;
    private HashSet<string> _knownValidPluginKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _pluginBaselineInitialized;

    public MainForm(YacaService? service = null)
    {
        _service = service ?? new YacaService();

        Text = Localization.Get(_service.Settings.Language).Title;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(760, 560);
        Size = new Size(980, 720);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);

        BuildUi();
        Shown += (_, _) => RefreshState();
    }

    private UiText Texts => Localization.Get(_service.Settings.Language);

    private void BuildUi()
    {
        // Dispose the previous visual tree before rebuilding. Controls.Clear() only
        // detaches controls; it does not dispose their native handles. Rebuilding
        // without disposal could leave stale plugin rows visible and cause duplicates.
        _root?.Dispose();
        _root = null;
        Controls.Clear();
        _pluginRows = null;
        _closeTeamSpeakButton = null;

        _current = new Label();
        _status = new Label();
        _paths = new Label();
        _plugins = new Panel();

        var text = Texts;
        Text = text.Title;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 16),
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        Controls.Add(root);
        _root = root;

        BuildHeader(root, text);
        BuildPaths(root, text);
        BuildCurrent(root, text);
        BuildStatus(root, text);
        BuildPluginList(root);
        BuildBottomButtons(root, text);
    }

    private static void BuildHeader(TableLayoutPanel root, UiText text)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));

        header.Controls.Add(new Label
        {
            Text = text.Title,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = Theme.Accent,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Theme.Background,
            AutoEllipsis = true,
            AutoSize = false,
            Padding = new Padding(0, 2, 10, 0)
        }, 0, 0);

        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        }, 1, 0);

        root.Controls.Add(header, 0, 0);
    }

    private void BuildPaths(TableLayoutPanel root, UiText text)
    {
        _paths.Dock = DockStyle.Fill;
        _paths.Font = new Font("Consolas", 9F);
        _paths.ForeColor = Theme.SecondaryForeground;
        _paths.BackColor = Theme.Background;
        _paths.AutoEllipsis = true;
        _paths.AutoSize = false;
        _paths.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_paths, 0, 1);
    }

    private void BuildCurrent(TableLayoutPanel root, UiText text)
    {
        var currentPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Padding = new Padding(14, 0, 14, 0),
            Margin = Padding.Empty
        };
        currentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        currentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        currentPanel.Controls.Add(new Label
        {
            Text = text.Active,
            Dock = DockStyle.Fill,
            ForeColor = Theme.SecondaryForeground,
            BackColor = Theme.Surface,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        _current.Dock = DockStyle.Fill;
        _current.Font = new Font("Segoe UI Semibold", 13F);
        _current.TextAlign = ContentAlignment.MiddleLeft;
        _current.BackColor = Theme.Surface;
        _current.AutoEllipsis = true;
        currentPanel.Controls.Add(_current, 1, 0);
        root.Controls.Add(currentPanel, 0, 2);
    }

    private void BuildStatus(TableLayoutPanel root, UiText text)
    {
        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty,
            Padding = new Padding(0, 2, 0, 2)
        };
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _status.Dock = DockStyle.Fill;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.ForeColor = Theme.Success;
        _status.BackColor = Theme.Background;
        _status.AutoEllipsis = true;
        statusPanel.Controls.Add(_status, 0, 0);

        _closeTeamSpeakButton = MakeButton(text.CloseTeamspeak, 210);
        _closeTeamSpeakButton.Visible = false;
        _closeTeamSpeakButton.ForeColor = Color.White;
        _closeTeamSpeakButton.BackColor = Color.FromArgb(150, 35, 35);
        _closeTeamSpeakButton.FlatAppearance.BorderColor = Color.FromArgb(190, 55, 55);
        _closeTeamSpeakButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(185, 45, 45);
        _closeTeamSpeakButton.Click += (_, _) => CloseTeamSpeak();
        statusPanel.Controls.Add(_closeTeamSpeakButton, 1, 0);

        root.Controls.Add(statusPanel, 0, 3);
    }

    private void BuildPluginList(TableLayoutPanel root)
    {
        _plugins.Dock = DockStyle.Fill;
        _plugins.AutoScroll = true;
        _plugins.BackColor = Theme.Surface;
        _plugins.ForeColor = Theme.Foreground;
        _plugins.Padding = new Padding(12, 10, 12, 10);
        _plugins.TabStop = true;
        DarkMode.ApplyScrollBarTheme(_plugins);

        _pluginRows = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 0,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _pluginRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _plugins.Controls.Add(_pluginRows);
        root.Controls.Add(_plugins, 0, 4);
    }

    private void BuildBottomButtons(TableLayoutPanel root, UiText text)
    {
        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty
        };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(0, 7, 0, 0),
            Margin = Padding.Empty
        };

        var refresh = MakeButton(text.Refresh, 105);
        refresh.Click += (_, _) => RefreshState(announceNewPlugins: true);
        var pluginFolder = MakeButton(text.PluginsFolderButton, 115);
        pluginFolder.Click += (_, _) => OpenPluginsFolder();
        var config = MakeButton(text.Config, 125);
        config.Click += (_, _) =>
        {
            using var dialog = new ConfigForm(_service);
            _ = dialog.ShowDialog(this);

            _pluginBaselineInitialized = false;
            _knownValidPluginKeys.Clear();

            // Rebuild the complete main UI after either Save or Cancel.
            // This guarantees that localization and configuration-dependent controls
            // are always synchronized with the current application state.
            BuildUi();
            RefreshState();
        };
        var backups = MakeButton(text.Backups, 95);
        backups.Click += (_, _) =>
        {
            using var dialog = new BackupForm(_service);
            dialog.ShowDialog(this);
            RefreshState();
        };
        var about = MakeButton(text.About, 75);
        about.Click += (_, _) =>
        {
            using var dialog = new AboutForm(_service.Settings.Language);
            dialog.ShowDialog(this);
        };
        var exit = MakeButton(text.Close, 90);
        exit.Click += (_, _) => Close();

        buttons.Controls.Add(refresh);
        buttons.Controls.Add(pluginFolder);
        buttons.Controls.Add(config);
        buttons.Controls.Add(backups);
        buttons.Controls.Add(about);
        buttons.Controls.Add(exit);
        bottom.Controls.Add(buttons, 1, 0);
        root.Controls.Add(bottom, 0, 5);
    }

    private static Button MakeButton(string text, int width)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 38,
            Margin = new Padding(5, 3, 5, 3),
            TabStop = true
        };
        Theme.StyleButton(button);
        return button;
    }

    private void RefreshState(bool announceNewPlugins = false)
    {
        var text = Texts;
        try
        {
            var current = _service.DetectCurrent();
            var plugins = _service.ScanPlugins()
                .GroupBy(GetPluginKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            _paths.Text = $"{text.Target}    {_service.TargetFile}{Environment.NewLine}{text.PluginsFolder} {_service.Paths.PluginDirectory}{Environment.NewLine}{text.BackupsPath} {_service.Paths.BackupDirectory}";
            _current.Text = current is null
                ? (File.Exists(_service.TargetFile) ? text.UnknownInvalid : text.NotInstalled)
                : current.DisplayName;
            _current.ForeColor = current is null ? Theme.Warning : Theme.Success;

            var running = TeamSpeakDetector.IsRunning();
            var pluginNotice = announceNewPlugins
                ? GetNewPluginNotice(plugins, text)
                : null;
            if (!_pluginBaselineInitialized)
                SetPluginBaseline(plugins);
            var baseStatus = running ? text.TeamspeakRunning : text.TeamspeakStopped;
            _status.Text = string.IsNullOrWhiteSpace(pluginNotice)
                ? baseStatus
                : $"{baseStatus} | {pluginNotice}";
            _status.ForeColor = running ? Theme.Error : Theme.Success;
            if (_closeTeamSpeakButton is not null)
            {
                _closeTeamSpeakButton.Visible = running;
                _closeTeamSpeakButton.Text = text.CloseTeamspeak;
            }

            if (_pluginRows is null)
                return;

            _pluginRows.SuspendLayout();
            try
            {
                _pluginRows.Controls.Clear();
                _pluginRows.RowStyles.Clear();
                _pluginRows.RowCount = 0;

                if (plugins.Count == 0)
                {
                    var emptyLabel = new Label
                    {
                        Text = text.NoPlugins,
                        ForeColor = Theme.Warning,
                        BackColor = Theme.Surface,
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Margin = new Padding(2, 2, 0, 8)
                    };
                    _pluginRows.RowCount = 1;
                    _pluginRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _pluginRows.Controls.Add(emptyLabel, 0, 0);
                }
                else
                {
                    foreach (var plugin in plugins)
                    {
                        var active = current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase);
                        var button = MakeButton(active ? $"{plugin.DisplayName}   —   {text.Active.TrimEnd(':')}" : plugin.DisplayName, 100);
                        button.Dock = DockStyle.Top;
                        button.Margin = new Padding(0, 0, 0, 8);
                        button.TextAlign = ContentAlignment.MiddleLeft;
                        button.ForeColor = active ? Theme.Success : Theme.Foreground;
                        button.BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control;
                        button.Click += (_, _) => Activate(plugin);

                        var row = _pluginRows.RowCount++;
                        _pluginRows.RowStyles.Add(new RowStyle(SizeType.Absolute, button.Height + button.Margin.Vertical));
                        _pluginRows.Controls.Add(button, 0, row);
                    }
                }
            }
            finally
            {
                _pluginRows.ResumeLayout(true);
            }
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"MainForm refresh failed: {ex}");
            _status.Text = "[ERROR] " + text.ErrorUnexpected;
            _status.ForeColor = Theme.Error;
            if (_closeTeamSpeakButton is not null)
                _closeTeamSpeakButton.Visible = false;
        }
    }


    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins, UiText text)
    {
        var currentKeys = plugins
            .Select(GetPluginKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!_pluginBaselineInitialized)
        {
            _knownValidPluginKeys = currentKeys;
            _pluginBaselineInitialized = true;
            return null;
        }

        var newPlugins = plugins
            .Where(plugin => !_knownValidPluginKeys.Contains(GetPluginKey(plugin)))
            .ToList();

        _knownValidPluginKeys = currentKeys;

        if (newPlugins.Count == 0)
            return null;

        return string.Format(
            CultureInfo.CurrentCulture,
            text.NewValidPluginFound,
            string.Join(", ", newPlugins.Select(plugin => plugin.DisplayName)));
    }

    private static string GetPluginKey(YacaPluginInfo plugin) =>
        $"{plugin.FilePath}|{plugin.Sha256}";

    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins)
    {
        _knownValidPluginKeys = plugins
            .Select(GetPluginKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _pluginBaselineInitialized = true;
    }

    private void CloseTeamSpeak()
    {
        var text = Texts;
        if (!TeamSpeakDetector.IsRunning())
        {
            RefreshState();
            return;
        }

        if (MessageBox.Show(this, text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            Cursor = Cursors.WaitCursor;
            var closed = TeamSpeakDetector.TryClose(TimeSpan.FromSeconds(5));
            if (!closed)
            {
                MessageBox.Show(this, text.CloseTeamspeakFailed, text.TeamspeakRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed));
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshState();
        }
    }

    private void Activate(YacaPluginInfo plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        var text = Texts;
        var current = _service.DetectCurrent();
        if (current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, $"{plugin.DisplayName} {text.AlreadyActiveMessage}", text.AlreadyActiveTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning())
        {
            if (MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups);
            MessageBox.Show(this, $"{plugin.DisplayName} {text.ActivatedMessage} {plugin.Sha256}", text.SuccessTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (UnauthorizedAccessException)
        {
            ShowError(text.AccessDenied);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or InvalidOperationException)
        {
            ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected));
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshState();
        }
    }

    private void OpenPluginsFolder()
    {
        try
        {
            Directory.CreateDirectory(_service.Paths.PluginDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_service.Paths.PluginDirectory}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            _service.Logger.Error($"Open plugins folder failed: {ex}");
            ShowError(Texts.OpenFolderError);
        }
    }

    private void ShowError(string message) => MessageBox.Show(this, message, Texts.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
