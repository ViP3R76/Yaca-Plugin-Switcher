using System.Diagnostics;
using System.Globalization;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;
using YacaPluginSwitcher.UI;

namespace YacaPluginSwitcher;

public sealed class ProfessionalMainForm : Form
{
    private readonly YacaService _service;
    private readonly Panel _pageHost = new();
    private readonly Label _pageTitle = new();
    private readonly Button _backButton = new();
    private readonly Button _refreshButton = new();
    private readonly Label _language = new();
    private Control? _page;
    private TableLayoutPanel? _switchRows;
    private Label? _currentValue;
    private Label? _ts3Value;
    private Button? _ts3Close;
    private Label? _backupValue;
    private Label? _versionsValue;
    private Label? _statusValue;
    private HashSet<string> _knownPlugins = new(StringComparer.OrdinalIgnoreCase);
    private bool _pluginBaselineInitialized;
    private string _activePage = "home";

    private UiText Texts => Localization.Get(_service.Settings.Language);
    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;

    public ProfessionalMainForm(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Text = Texts.Title;
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Font;
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(980, 700);
        Size = new Size(1180, 800);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);
        BuildShell();
        Shown += (_, _) => RefreshHome();
    }

    private void BuildShell()
    {
        SuspendLayout();
        Controls.Clear();
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
            Padding = new Padding(26, 20, 26, 20), BackColor = Theme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);
        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildNavigation(), 0, 1);
        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = Theme.Background;
        _pageHost.Padding = Padding.Empty;
        root.Controls.Add(_pageHost, 0, 2);
        ShowHome();
        ResumeLayout(true);
    }

    private static TableLayoutPanel BuildHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1,
            BackColor = Theme.Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        var titlePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Theme.Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        titlePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        titlePanel.Controls.Add(new Label
        {
            Text = "YACA Plugin Switcher", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 25F), ForeColor = Theme.Accent,
            BackColor = Theme.Background, TextAlign = ContentAlignment.BottomLeft,
            AutoEllipsis = true
        }, 0, 0);
        titlePanel.Controls.Add(new Label
        {
            Text = "by ViP3R_76", Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10.5F), ForeColor = Theme.BrandGold,
            BackColor = Theme.Background, TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);
        header.Controls.Add(titlePanel, 0, 0);
        header.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo, BackColor = Theme.Background,
            Margin = new Padding(8, 2, 0, 2)
        }, 1, 0);
        return header;
    }

    private TableLayoutPanel BuildNavigation()
    {
        var nav = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1,
            BackColor = Theme.Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        nav.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _backButton.Text = IsGerman ? "Zurück" : "Back";
        _backButton.Width = 96; _backButton.Height = 34; _backButton.Visible = false;
        Theme.StyleButton(_backButton);
        _backButton.Click += (_, _) => ShowHome();
        nav.Controls.Add(_backButton, 0, 0);
        _pageTitle.Text = IsGerman ? "Übersicht" : "Overview";
        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.TextAlign = ContentAlignment.MiddleCenter;
        _pageTitle.Font = new Font("Segoe UI Semibold", 11F);
        _pageTitle.ForeColor = Theme.SecondaryForeground;
        nav.Controls.Add(_pageTitle, 1, 0);
        _refreshButton.Text = IsGerman ? "Aktualisieren" : "Refresh";
        _refreshButton.Width = 112; _refreshButton.Height = 34;
        _refreshButton.Margin = new Padding(4, 1, 10, 1);
        Theme.StyleButton(_refreshButton);
        _refreshButton.Click += (_, _) => RefreshActivePage(true);
        nav.Controls.Add(_refreshButton, 2, 0);
        _language.Text = IsGerman ? "DE" : "EN";
        _language.AutoSize = true; _language.Dock = DockStyle.Fill;
        _language.TextAlign = ContentAlignment.MiddleRight;
        _language.ForeColor = Theme.SecondaryForeground;
        nav.Controls.Add(_language, 3, 0);
        return nav;
    }

    private void ShowHome()
    {
        _activePage = "home"; _switchRows = null;
        _pageHost.Controls.Clear();
        _page = BuildDashboard();
        _pageHost.Controls.Add(_page); _page.Dock = DockStyle.Fill;
        _backButton.Visible = false; _refreshButton.Visible = true;
        _pageTitle.Text = IsGerman ? "Übersicht" : "Overview";
        _language.Text = IsGerman ? "DE" : "EN"; Text = Texts.Title;
        RefreshHome();
    }

    private TableLayoutPanel BuildDashboard()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4,
            BackColor = Theme.Background, Margin = Padding.Empty, Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        var status = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _currentValue = AddStatusCard(status, 0, IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", Theme.Accent);
        (_ts3Value, _ts3Close) = AddTs3Card(status, 1); root.Controls.Add(status, 0, 0);
        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        for (var i = 0; i < 5; i++) actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        AddAction(actions, 0, "⇄", IsGerman ? "YACA wechseln" : "Switch YACA", IsGerman ? "Version auswählen und aktivieren" : "Select and activate a version", Theme.Accent, ShowSwitchPage, true);
        AddAction(actions, 1, "+", IsGerman ? "Backup erstellen" : "Create Backup", IsGerman ? "Aktuell installierte Version sichern" : "Save the installed version", Theme.Accent, CreateBackupFromDashboard, false);
        AddAction(actions, 2, "◉", Texts.Backups, IsGerman ? "Verwalten und wiederherstellen" : "Manage and restore", Theme.BrandGold, ShowBackups, false);
        AddAction(actions, 3, "⚙", Texts.Config, IsGerman ? "Anwendung konfigurieren" : "Configure the application", Theme.BrandGold, ShowConfig, false);
        AddAction(actions, 4, "ⓘ", Texts.About, IsGerman ? "YACA, TeamSpeak und Community" : "YACA, TeamSpeak and community", Theme.BrandGold, ShowInfo, false);
        root.Controls.Add(actions, 0, 1);
        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42)); lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _backupValue = AddInfoCard(lower, 0, IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP");
        _versionsValue = AddInfoCard(lower, 1, IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS");
        root.Controls.Add(lower, 0, 2);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Surface, Margin = new Padding(0, 6, 0, 0) };
        for (var i = 0; i < 3; i++) footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        AddFooter(footer, 0, "BACKUPS", () => $"{_service.Settings.MaxBackups} / " + (IsGerman ? "maximal" : "maximum"), Theme.Accent);
        AddFooter(footer, 1, "LOGS", () => IsGerman ? "3 Tage Aufbewahrung" : "3-day retention", Theme.BrandGold);
        _statusValue = AddFooter(footer, 2, "STATUS", () => IsGerman ? "Bereit" : "Ready", Theme.Success);
        root.Controls.Add(footer, 0, 3); return root;
    }

    private static Label AddStatusCard(TableLayoutPanel host, int column, string title, Color accent)
    {
        var card = Card(accent);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 12, 18, 12) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 18F), ForeColor = accent, BackColor = Theme.Surface, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(value, 0, 1); card.Controls.Add(table); host.Controls.Add(card, column, 0); return value;
    }

    private (Label Value, Button Close) AddTs3Card(TableLayoutPanel host, int column)
    {
        var card = Card(Theme.Success);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 10, 18, 10) };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24)); table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var heading = new Label { Text = "TEAMSPEAK 3", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(heading, 0, 0); table.SetColumnSpan(heading, 2);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 15F), ForeColor = Theme.Success, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(value, 0, 1);
        var close = new Button { Text = "", Width = 150, Height = 34, Visible = false, Margin = new Padding(6, 2, 0, 0) };
        Theme.StyleButton(close); close.BackColor = Color.FromArgb(105, 70, 10); close.FlatAppearance.BorderColor = Theme.BrandGold; close.Click += (_, _) => CloseTeamSpeak();
        table.Controls.Add(close, 1, 1); card.Controls.Add(table); host.Controls.Add(card, column, 0); return (value, close);
    }

    private static void AddAction(TableLayoutPanel host, int column, string icon, string title, string subtitle, Color accent, Action action, bool primary)
    {
        var button = new Button { Dock = DockStyle.Fill, Margin = new Padding(5), FlatStyle = FlatStyle.Flat, BackColor = primary ? Color.FromArgb(38, 35, 52) : Theme.Surface, ForeColor = Theme.Foreground, Font = new Font("Segoe UI Semibold", primary ? 11F : 10F), Text = $"{icon}  {title}\n{subtitle}", TextAlign = ContentAlignment.MiddleCenter, Cursor = Cursors.Hand };
        button.FlatAppearance.BorderColor = accent; button.FlatAppearance.BorderSize = primary ? 2 : 1; button.FlatAppearance.MouseOverBackColor = Theme.ControlHover; button.Click += (_, _) => action(); host.Controls.Add(button, column, 0);
    }

    private static Label AddInfoCard(TableLayoutPanel host, int column, string title)
    {
        var card = Card(Theme.Control);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(18, 12, 18, 12) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26)); table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 9F), ForeColor = Theme.SecondaryForeground, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), ForeColor = Theme.Foreground, BackColor = Theme.Surface, AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft };
        table.Controls.Add(value, 0, 1); card.Controls.Add(table); host.Controls.Add(card, column, 0); return value;
    }

    private static Label AddFooter(TableLayoutPanel host, int column, string title, Func<string> value, Color accent)
    {
        var label = new Label { Dock = DockStyle.Fill, BackColor = Theme.Surface, ForeColor = accent, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 8.5F) };
        label.Text = $"{title}\n{value()}"; host.Controls.Add(label, column, 0); return label;
    }

    private static Panel Card(Color accent)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(5), Padding = new Padding(1) };
        panel.Paint += (_, e) => { using var pen = new Pen(accent, 1); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, panel.Width - 1), Math.Max(0, panel.Height - 1)); };
        return panel;
    }

    private void ShowSwitchPage()
    {
        _activePage = "switch"; _pageHost.Controls.Clear(); _switchRows = null; _page = BuildSwitchPage(); _pageHost.Controls.Add(_page); _page.Dock = DockStyle.Fill;
        _backButton.Visible = true; _refreshButton.Visible = true; _pageTitle.Text = IsGerman ? "YACA wechseln" : "Switch YACA"; RefreshSwitchPage(false);
    }

    private TableLayoutPanel BuildSwitchPage()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Background, Margin = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = IsGerman ? "Verfügbare valide YACA-DLLs" : "Available valid YACA DLLs", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Theme.SecondaryForeground, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(14), AutoScroll = true }; DarkMode.ApplyScrollBarTheme(host);
        _switchRows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, BackColor = Theme.Surface };
        _switchRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); host.Controls.Add(_switchRows); root.Controls.Add(host, 0, 1); return root;
    }

    private void ShowBackups() => ShowEmbeddedForm(new BackupForm(_service), Texts.Backups);
    private void ShowConfig() => ShowEmbeddedForm(new ConfigForm(_service), Texts.ConfigTitle);

    private void ShowEmbeddedForm(Form form, string title)
    {
        _activePage = "embedded"; _pageHost.Controls.Clear(); _page = form;
        form.TopLevel = false; form.FormBorderStyle = FormBorderStyle.None; form.ShowInTaskbar = false; form.MinimumSize = Size.Empty; form.MaximumSize = Size.Empty; form.Dock = DockStyle.Fill; form.AutoScroll = true; form.Margin = Padding.Empty; form.Parent = _pageHost; form.Show(); form.BringToFront();
        _backButton.Visible = true; _refreshButton.Visible = false; _pageTitle.Text = title; form.FormClosed += (_, _) => ShowHome();
    }

    private void ShowInfo()
    {
        _activePage = "info"; _pageHost.Controls.Clear(); _page = new InfoPage(_service.Settings.Language); _pageHost.Controls.Add(_page); _page.Dock = DockStyle.Fill;
        _backButton.Visible = true; _refreshButton.Visible = false; _pageTitle.Text = Texts.AboutTitle;
    }

    private void RefreshActivePage(bool announce)
    {
        if (_activePage == "home") RefreshHome(announce); else if (_activePage == "switch") RefreshSwitchPage(announce);
    }

    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home") return;
        try
        {
            var text = Texts; var current = _service.DetectCurrent(); var plugins = _service.ScanPlugins().GroupBy(GetPluginKey, StringComparer.OrdinalIgnoreCase).Select(g => g[0]).ToList();
            var notice = announce ? GetNewPluginNotice(plugins, text) : null; if (!_pluginBaselineInitialized) SetPluginBaseline(plugins);
            _currentValue!.Text = current?.DisplayName ?? (File.Exists(_service.TargetFile) ? text.UnknownInvalid : text.NotInstalled); _currentValue.ForeColor = current is null ? Theme.Warning : Theme.Success;
            var running = TeamSpeakDetector.IsRunning(); _ts3Value!.Text = running ? (IsGerman ? "● GESTARTET" : "● RUNNING") : (IsGerman ? "✓ NICHT GESTARTET" : "✓ NOT RUNNING"); _ts3Value.ForeColor = running ? Theme.Error : Theme.Success; _ts3Close!.Visible = running; _ts3Close.Text = text.CloseTeamspeak;
            var backups = _service.Backups.ListBackups(); var backup = backups.FirstOrDefault(); _backupValue!.Text = backup is null ? text.NoBackups : $"{backup.Timestamp:dd.MM.yyyy HH:mm:ss}\n{backup.DisplayName}\n{backup.FileSize:N0} Bytes";
            _versionsValue!.Text = plugins.Count == 0 ? text.NoPlugins : string.Join(Environment.NewLine, plugins.Select(p => p.DisplayName)); _statusValue!.Text = string.IsNullOrWhiteSpace(notice) ? (running ? text.TeamspeakRunning : text.TeamspeakStopped) : notice;
        }
        catch (Exception ex) { _service.Logger.Error($"Dashboard refresh failed: {ex}"); if (_statusValue is not null) _statusValue.Text = "[ERROR] " + Texts.ErrorUnexpected; }
    }

    private void RefreshSwitchPage(bool announce)
    {
        if (_switchRows is null) return;
        try
        {
            var text = Texts; var current = _service.DetectCurrent(); var plugins = _service.ScanPlugins().GroupBy(GetPluginKey, StringComparer.OrdinalIgnoreCase).Select(g => g[0]).ToList(); var notice = announce ? GetNewPluginNotice(plugins, text) : null; if (!_pluginBaselineInitialized) SetPluginBaseline(plugins);
            _switchRows.SuspendLayout(); _switchRows.Controls.Clear(); _switchRows.RowStyles.Clear(); _switchRows.RowCount = 0;
            if (!string.IsNullOrWhiteSpace(notice)) AddSwitchLabel(notice, Theme.Success); if (plugins.Count == 0) AddSwitchLabel(text.NoPlugins, Theme.Warning);
            foreach (var plugin in plugins)
            {
                var active = current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase);
                var button = new Button { Text = active ? $"{plugin.DisplayName}   —   {text.Active.TrimEnd(':')}" : plugin.DisplayName, Height = 48, Dock = DockStyle.Top, Margin = new Padding(0, 0, 0, 8), TextAlign = ContentAlignment.MiddleLeft, BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control, ForeColor = active ? Theme.Success : Theme.Foreground };
                Theme.StyleButton(button); button.BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control; button.ForeColor = active ? Theme.Success : Theme.Foreground; button.Click += (_, _) => Activate(plugin);
                _switchRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 56)); _switchRows.Controls.Add(button, 0, _switchRows.RowCount++);
            }
            _switchRows.ResumeLayout(true);
        }
        catch (Exception ex) { _service.Logger.Error($"Switch page refresh failed: {ex}"); ShowError(Texts.ErrorUnexpected); }
    }

    private void AddSwitchLabel(string text, Color color) { _switchRows!.RowStyles.Add(new RowStyle(SizeType.AutoSize)); _switchRows.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = color, BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 10) }, 0, _switchRows.RowCount++); }

    private void Activate(YacaPluginInfo plugin)
    {
        var text = Texts; var current = _service.DetectCurrent(); if (current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase)) { MessageBox.Show(this, $"{plugin.DisplayName} {text.AlreadyActiveMessage}", text.AlreadyActiveTitle, MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning() && MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { Cursor = Cursors.WaitCursor; _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException) { _service.Logger.Error($"YACA switch failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); }
        finally { Cursor = Cursors.Default; RefreshSwitchPage(false); }
    }

    private void CreateBackupFromDashboard()
    {
        var text = Texts; if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning && MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { var current = _service.DetectCurrent(); if (current is null) { ShowError(text.NotInstalled); return; } if (_service.Backups.CreateBackup(_service.TargetFile, current) is null) { ShowError(text.ErrorUnexpected); return; } _service.Backups.Trim(_service.Settings.MaxBackups); RefreshHome(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException) { _service.Logger.Error($"Dashboard backup failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); }
    }

    private void CloseTeamSpeak()
    {
        var text = Texts; if (!TeamSpeakDetector.IsRunning()) { RefreshHome(); return; }
        if (MessageBox.Show(this, text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try { Cursor = Cursors.WaitCursor; if (!TeamSpeakDetector.TryClose(TimeSpan.FromSeconds(5))) MessageBox.Show(this, text.CloseTeamspeakFailed, text.TeamspeakRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed)); }
        finally { Cursor = Cursors.Default; RefreshHome(); }
    }

    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins, UiText text)
    {
        var keys = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase); if (!_pluginBaselineInitialized) { _knownPlugins = keys; _pluginBaselineInitialized = true; return null; }
        var added = plugins.Where(p => !_knownPlugins.Contains(GetPluginKey(p))).ToList(); _knownPlugins = keys; return added.Count == 0 ? null : string.Format(CultureInfo.CurrentCulture, text.NewValidPluginFound, string.Join(", ", added.Select(p => p.DisplayName)));
    }

    private static string GetPluginKey(YacaPluginInfo plugin) => $"{plugin.FilePath}|{plugin.Sha256}";
    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins) { _knownPlugins = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase); _pluginBaselineInitialized = true; }
    private void ShowError(string message) => MessageBox.Show(this, message, Texts.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
