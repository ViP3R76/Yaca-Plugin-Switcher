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
    private readonly Panel _pageHost = new();
    private readonly Label _pageTitle = new();
    private readonly Button _backButton = new();
    private readonly Label _languageIndicator = new();
    private readonly Button _refreshButton = new();
    private Form? _embeddedPage;
    private Panel? _dashboard;
    private Panel? _switchPage;
    private TableLayoutPanel? _switchRows;
    private Label? _dashboardCurrent;
    private Label? _dashboardTs3;
    private Label? _dashboardBackup;
    private Label? _dashboardVersions;
    private Label? _dashboardStatus;
    private HashSet<string> _knownValidPluginKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _pluginBaselineInitialized;
    private string _activePage = "home";

    private UiText Texts => Localization.Get(_service.Settings.Language);
    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;
    private string OverviewText => IsGerman ? "Übersicht" : "Overview";
    private string SwitchText => IsGerman ? "Wechseln" : "Switch";
    private string BackText => IsGerman ? "Zurück" : "Back";
    private string BackupCreateText => IsGerman ? "Backup erstellen" : "Create Backup";

    public MainForm(YacaService? service = null)
    {
        _service = service ?? new YacaService();

        Text = Texts.Title;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        MinimumSize = new Size(900, 640);
        Size = new Size(1180, 820);
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);

        BuildShell();
        Shown += (_, _) => RefreshDashboard();
    }

    private void BuildShell()
    {
        Controls.Clear();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));

        header.Controls.Add(new Label
        {
            Text = "YACA\nPLUGIN SWITCHER",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 18F),
            ForeColor = Theme.Foreground,
            BackColor = Theme.Background,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);

        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo,
            BackColor = Theme.Background,
            Margin = Padding.Empty
        }, 1, 0);

        header.Controls.Add(new Label
        {
            Text = "by ViP3R_76",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 11F),
            ForeColor = Theme.BrandGold,
            BackColor = Theme.Background,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 4, 0)
        }, 2, 0);
        root.Controls.Add(header, 0, 0);

        var navigation = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        navigation.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _backButton.Text = BackText;
        _backButton.Width = 100;
        _backButton.Height = 36;
        _backButton.Visible = false;
        Theme.StyleButton(_backButton);
        _backButton.Click += (_, _) => ShowHome();
        navigation.Controls.Add(_backButton, 0, 0);

        _pageTitle.Text = OverviewText;
        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.Font = new Font("Segoe UI Semibold", 11F);
        _pageTitle.ForeColor = Theme.SecondaryForeground;
        _pageTitle.TextAlign = ContentAlignment.MiddleCenter;
        navigation.Controls.Add(_pageTitle, 1, 0);

        _refreshButton.Text = IsGerman ? "Aktualisieren" : "Refresh";
        _refreshButton.Width = 110;
        _refreshButton.Height = 34;
        _refreshButton.Margin = new Padding(4, 1, 8, 1);
        Theme.StyleButton(_refreshButton);
        _refreshButton.Click += (_, _) => RefreshCurrentPage(true);
        navigation.Controls.Add(_refreshButton, 2, 0);

        _languageIndicator.Text = IsGerman ? "DE" : "EN";
        _languageIndicator.AutoSize = true;
        _languageIndicator.Dock = DockStyle.Fill;
        _languageIndicator.ForeColor = Theme.SecondaryForeground;
        _languageIndicator.TextAlign = ContentAlignment.MiddleRight;
        _languageIndicator.Padding = new Padding(0, 0, 4, 0);
        navigation.Controls.Add(_languageIndicator, 3, 0);
        root.Controls.Add(navigation, 0, 1);

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = Theme.Background;
        _pageHost.Padding = Padding.Empty;
        root.Controls.Add(_pageHost, 0, 2);

        ShowHome();
    }

    private void ShowHome()
    {
        if (_embeddedPage is not null)
        {
            var page = _embeddedPage;
            _embeddedPage = null;
            page.FormClosed -= EmbeddedPageClosed;
            page.Close();
        }

        _pageHost.Controls.Clear();
        _switchPage = null;
        _switchRows = null;
        _dashboard = BuildDashboard();
        _pageHost.Controls.Add(_dashboard);
        _dashboard.Dock = DockStyle.Fill;
        _backButton.Text = BackText;
        _backButton.Visible = false;
        _pageTitle.Text = OverviewText;
        _languageIndicator.Text = IsGerman ? "DE" : "EN";
        _refreshButton.Text = IsGerman ? "Aktualisieren" : "Refresh";
        _activePage = "home";
        RefreshDashboard();
    }

    private Panel BuildDashboard()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background, Padding = Padding.Empty };
        _dashboard = page;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        page.Controls.Add(root);

        var statusCards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        statusCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        statusCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _dashboardCurrent = AddStatusCard(statusCards, 0, IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", "—", Theme.Accent);
        _dashboardTs3 = AddTs3Card(statusCards, 1);
        root.Controls.Add(statusCards, 0, 0);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        for (var i = 0; i < 5; i++)
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        AddActionCard(actions, 0, "⇄", SwitchText, IsGerman ? "Version wechseln" : "Switch version", Theme.Accent, ShowSwitchPage);
        AddActionCard(actions, 1, "+", BackupCreateText, IsGerman ? "Aktuelle Version sichern" : "Save current version", Theme.Accent, CreateBackupFromDashboard);
        AddActionCard(actions, 2, "◉", Texts.Backups, IsGerman ? "Verwalten & wiederherstellen" : "Manage & restore", Theme.Accent, () => ShowEmbeddedPage(new BackupForm(_service), Texts.Backups));
        AddActionCard(actions, 3, "⚙", Texts.Config, IsGerman ? "Optionen konfigurieren" : "Configure options", Theme.BrandGold, () => ShowEmbeddedPage(new ConfigForm(_service), Texts.ConfigTitle));
        AddActionCard(actions, 4, "ⓘ", Texts.About, IsGerman ? "YACA / TeamSpeak / Community" : "YACA / TeamSpeak / Community", Theme.BrandGold, () => ShowEmbeddedPage(new AboutForm(_service.Settings.Language), Texts.AboutTitle));
        root.Controls.Add(actions, 0, 1);

        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        _dashboardBackup = AddInfoCard(lower, 0, IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP");
        _dashboardVersions = AddInfoCard(lower, 1, IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS");
        root.Controls.Add(lower, 0, 2);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, BackColor = Theme.Surface, Margin = new Padding(0, 6, 0, 0) };
        for (var i = 0; i < 4; i++)
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        AddFooterStatus(footer, 0, "BACKUPS", () => _service.Settings.AutomaticBackup ? (IsGerman ? "Automatisch aktiv" : "Automatic on") : (IsGerman ? "Automatisch aus" : "Automatic off"), _service.Settings.AutomaticBackup ? Theme.Success : Theme.Warning);
        AddFooterStatus(footer, 1, IsGerman ? "AUFBEWAHRUNG" : "RETENTION", () => $"{_service.Settings.MaxBackups} Backups", Theme.Accent);
        AddFooterStatus(footer, 2, "LOGS", () => IsGerman ? "3 Tage" : "3 days", Theme.BrandGold);
        AddFooterStatus(footer, 3, IsGerman ? "SPRACHE" : "LANGUAGE", () => IsGerman ? "Deutsch" : "English", Theme.Accent);
        root.Controls.Add(footer, 0, 3);

        return page;
    }

    private Label AddStatusCard(TableLayoutPanel host, int column, string title, string value, Color accent)
    {
        var panel = MakeCard(accent);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 12, 18, 12) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, Font = new Font("Segoe UI Semibold", 9F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var valueLabel = new Label { Text = value, Dock = DockStyle.Fill, ForeColor = accent, BackColor = Theme.Surface, Font = new Font("Segoe UI Semibold", 18F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        table.Controls.Add(valueLabel, 0, 1);
        panel.Controls.Add(table);
        host.Controls.Add(panel, column, 0);
        return valueLabel;
    }

    private Label AddTs3Card(TableLayoutPanel host, int column)
    {
        var panel = MakeCard(Theme.Success);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 10, 18, 10) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new Label { Text = "TEAMSPEAK 3", Dock = DockStyle.Fill, ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, Font = new Font("Segoe UI Semibold", 9F), TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(heading, 0, 0);
        table.SetColumnSpan(heading, 2);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, ForeColor = Theme.Success, BackColor = Theme.Surface, Font = new Font("Segoe UI Semibold", 15F), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true };
        table.Controls.Add(value, 0, 1);
        var close = new Button { Text = Texts.CloseTeamspeak, Width = 150, Height = 34, Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Visible = false, Margin = new Padding(6, 2, 0, 0) };
        Theme.StyleButton(close);
        close.BackColor = Color.FromArgb(105, 70, 10);
        close.FlatAppearance.BorderColor = Theme.BrandGold;
        close.Click += (_, _) => CloseTeamSpeak();
        table.Controls.Add(close, 1, 1);
        value.Tag = close;
        panel.Controls.Add(table);
        host.Controls.Add(panel, column, 0);
        return value;
    }

    private void AddActionCard(TableLayoutPanel host, int column, string icon, string title, string subtitle, Color accent, Action action)
    {
        var button = new Button
        {
            Text = $"{icon}  {title}\n{subtitle}",
            Dock = DockStyle.Fill,
            Height = 110,
            Margin = new Padding(5),
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            Font = new Font("Segoe UI Semibold", 10F),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.MouseOverBackColor = Theme.ControlHover;
        button.FlatAppearance.BorderSize = 1;
        button.Click += (_, _) => action();
        host.Controls.Add(button, column, 0);
    }

    private Label AddInfoCard(TableLayoutPanel host, int column, string title)
    {
        var panel = MakeCard(Theme.Control);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 12, 18, 12) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, Font = new Font("Segoe UI Semibold", 9F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, ForeColor = Theme.Foreground, BackColor = Theme.Surface, Font = new Font("Segoe UI", 10F), TextAlign = ContentAlignment.TopLeft, AutoEllipsis = true };
        table.Controls.Add(value, 0, 1);
        panel.Controls.Add(table);
        host.Controls.Add(panel, column, 0);
        return value;
    }

    private static Panel MakeCard(Color accent)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(5), Padding = new Padding(1) };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(accent, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, panel.Width - 1), Math.Max(0, panel.Height - 1));
        };
        return panel;
    }

    private void AddFooterStatus(TableLayoutPanel host, int column, string title, Func<string> value, Color accent)
    {
        var label = new Label { Dock = DockStyle.Fill, BackColor = Theme.Surface, ForeColor = accent, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F) };
        label.Text = $"{title}\n{value()}";
        host.Controls.Add(label, column, 0);
    }

    private void RefreshCurrentPage(bool announceNewPlugins)
    {
        if (_activePage == "switch")
        {
            RefreshSwitchPage(announceNewPlugins);
            return;
        }

        if (_activePage == "home")
            RefreshDashboard();
    }

    private void ShowSwitchPage()
    {
        if (_embeddedPage is not null)
            return;
        _pageHost.Controls.Clear();
        _switchPage = BuildSwitchPage();
        _pageHost.Controls.Add(_switchPage);
        _switchPage.Dock = DockStyle.Fill;
        _backButton.Visible = true;
        _pageTitle.Text = SwitchText;
        _activePage = "switch";
        RefreshSwitchPage(false);
    }

    private Panel BuildSwitchPage()
    {
        var page = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Background, Padding = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        page.Controls.Add(root);

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.Controls.Add(new Label { Text = IsGerman ? "YACA Versionen" : "YACA Versions", Dock = DockStyle.Fill, ForeColor = Theme.SecondaryForeground, Font = new Font("Segoe UI Semibold", 11F), TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var refresh = new Button { Text = Texts.Refresh, Width = 110, Height = 34, Margin = new Padding(4) };
        Theme.StyleButton(refresh);
        refresh.Click += (_, _) => RefreshSwitchPage(true);
        top.Controls.Add(refresh, 1, 0);
        root.Controls.Add(top, 0, 0);

        var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(14), AutoScroll = true };
        DarkMode.ApplyScrollBarTheme(host);
        _switchRows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, BackColor = Theme.Surface, ForeColor = Theme.Foreground };
        _switchRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.Controls.Add(_switchRows);
        root.Controls.Add(host, 0, 1);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Background };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var folder = new Button { Text = Texts.PluginsFolderButton, Width = 140, Height = 36 };
        Theme.StyleButton(folder);
        folder.Click += (_, _) => OpenPluginsFolder();
        bottom.Controls.Add(folder, 1, 0);
        root.Controls.Add(bottom, 0, 2);
        return page;
    }

    private void RefreshSwitchPage(bool announceNewPlugins)
    {
        if (_switchRows is null)
            return;
        var text = Texts;
        try
        {
            var current = _service.DetectCurrent();
            var plugins = _service.ScanPlugins().GroupBy(GetPluginKey, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
            var notice = announceNewPlugins ? GetNewPluginNotice(plugins, text) : null;
            if (!_pluginBaselineInitialized)
                SetPluginBaseline(plugins);

            _switchRows.SuspendLayout();
            try
            {
                _switchRows.Controls.Clear();
                _switchRows.RowStyles.Clear();
                _switchRows.RowCount = 0;
                if (!string.IsNullOrWhiteSpace(notice))
                {
                    _switchRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _switchRows.Controls.Add(new Label { Text = notice, AutoSize = true, ForeColor = Theme.Success, BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 10) }, 0, _switchRows.RowCount++);
                }

                if (plugins.Count == 0)
                {
                    _switchRows.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    _switchRows.Controls.Add(new Label { Text = text.NoPlugins, AutoSize = true, ForeColor = Theme.Warning, BackColor = Theme.Surface, Margin = new Padding(0, 2, 0, 8) }, 0, _switchRows.RowCount++);
                }
                else
                {
                    foreach (var plugin in plugins)
                    {
                        var active = current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase);
                        var button = new Button
                        {
                            Text = active ? $"{plugin.DisplayName}   —   {text.Active.TrimEnd(':')}" : plugin.DisplayName,
                            Height = 46,
                            Dock = DockStyle.Top,
                            Margin = new Padding(0, 0, 0, 8),
                            TextAlign = ContentAlignment.MiddleLeft,
                            ForeColor = active ? Theme.Success : Theme.Foreground,
                            BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control
                        };
                        Theme.StyleButton(button);
                        button.ForeColor = active ? Theme.Success : Theme.Foreground;
                        button.BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control;
                        button.Click += (_, _) => Activate(plugin);
                        _switchRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
                        _switchRows.Controls.Add(button, 0, _switchRows.RowCount++);
                    }
                }
            }
            finally
            {
                _switchRows.ResumeLayout(true);
            }
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Switch page refresh failed: {ex}");
            ShowError(text.ErrorUnexpected);
        }
    }

    private void ShowEmbeddedPage(Form page, string title)
    {
        if (_embeddedPage is not null)
            return;
        _pageHost.Controls.Clear();
        _embeddedPage = page;
        _embeddedPage.FormClosed += EmbeddedPageClosed;
        _embeddedPage.TopLevel = false;
        _embeddedPage.FormBorderStyle = FormBorderStyle.None;
        _embeddedPage.ControlBox = false;
        _embeddedPage.ShowInTaskbar = false;
        _embeddedPage.Dock = DockStyle.Fill;
        _embeddedPage.Margin = Padding.Empty;
        _embeddedPage.MinimumSize = Size.Empty;
        _embeddedPage.MaximumSize = Size.Empty;
        _pageHost.Controls.Add(_embeddedPage);
        _embeddedPage.Show();
        _embeddedPage.BringToFront();
        _backButton.Text = BackText;
        _backButton.Visible = true;
        _pageTitle.Text = title;
        _languageIndicator.Text = IsGerman ? "DE" : "EN";
        _activePage = "embedded";
    }

    private void EmbeddedPageClosed(object? sender, FormClosedEventArgs e)
    {
        if (ReferenceEquals(sender, _embeddedPage))
            _embeddedPage = null;
        ShowHome();
    }

    private void CreateBackupFromDashboard()
    {
        var text = Texts;
        if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning)
        {
            if (MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }

        try
        {
            var current = _service.DetectCurrent();
            if (current is null)
            {
                ShowError(text.NotInstalled);
                return;
            }
            if (_service.Backups.CreateBackup(_service.TargetFile, current) is null)
            {
                ShowError(text.ErrorUnexpected);
                return;
            }
            RefreshDashboard();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _service.Logger.Error($"Dashboard backup failed: {ex}");
            ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected));
        }
    }

    private void RefreshDashboard()
    {
        if (_activePage != "home" || _dashboard is null)
            return;
        var text = Texts;
        try
        {
            var current = _service.DetectCurrent();
            var plugins = _service.ScanPlugins().GroupBy(GetPluginKey, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
            if (!_pluginBaselineInitialized)
                SetPluginBaseline(plugins);

            _dashboardCurrent!.Text = current?.DisplayName ?? (File.Exists(_service.TargetFile) ? text.UnknownInvalid : text.NotInstalled);
            _dashboardCurrent.ForeColor = current is null ? Theme.Warning : Theme.Success;

            var running = TeamSpeakDetector.IsRunning();
            _dashboardTs3!.Text = running ? "● GESTARTET" : "✓ NICHT GESTARTET";
            if (!IsGerman)
                _dashboardTs3.Text = running ? "● RUNNING" : "✓ NOT RUNNING";
            _dashboardTs3.ForeColor = running ? Theme.Error : Theme.Success;
            if (_dashboardTs3.Tag is Button closeButton)
            {
                closeButton.Visible = running;
                closeButton.Text = text.CloseTeamspeak;
            }

            var backup = _service.Backups.ListBackups().FirstOrDefault();
            _dashboardBackup!.Text = backup is null
                ? text.NoBackups
                : $"{backup.Timestamp:dd.MM.yyyy HH:mm:ss}\n{backup.DisplayName}\n{backup.FileSize:N0} Bytes";
            _dashboardVersions!.Text = plugins.Count == 0 ? text.NoPlugins : string.Join(Environment.NewLine, plugins.Select(p => p.DisplayName));
            _dashboardStatus!.Text = running ? text.TeamspeakRunning : text.TeamspeakStopped;
            Text = text.Title;
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Dashboard refresh failed: {ex}");
            if (_dashboardStatus is not null)
                _dashboardStatus.Text = "[ERROR] " + text.ErrorUnexpected;
        }
    }

    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins, UiText text)
    {
        var currentKeys = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!_pluginBaselineInitialized)
        {
            _knownValidPluginKeys = currentKeys;
            _pluginBaselineInitialized = true;
            return null;
        }

        var newPlugins = plugins.Where(plugin => !_knownValidPluginKeys.Contains(GetPluginKey(plugin))).ToList();
        _knownValidPluginKeys = currentKeys;
        return newPlugins.Count == 0
            ? null
            : string.Format(CultureInfo.CurrentCulture, text.NewValidPluginFound, string.Join(", ", newPlugins.Select(p => p.DisplayName)));
    }

    private static string GetPluginKey(YacaPluginInfo plugin) => $"{plugin.FilePath}|{plugin.Sha256}";

    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins)
    {
        _knownValidPluginKeys = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _pluginBaselineInitialized = true;
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
            RefreshSwitchPage(false);
            RefreshDashboard();
        }
    }

    private void CloseTeamSpeak()
    {
        var text = Texts;
        if (!TeamSpeakDetector.IsRunning())
        {
            RefreshDashboard();
            return;
        }

        if (MessageBox.Show(this, text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        try
        {
            Cursor = Cursors.WaitCursor;
            if (!TeamSpeakDetector.TryClose(TimeSpan.FromSeconds(5)))
                MessageBox.Show(this, text.CloseTeamspeakFailed, text.TeamspeakRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed));
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshDashboard();
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
