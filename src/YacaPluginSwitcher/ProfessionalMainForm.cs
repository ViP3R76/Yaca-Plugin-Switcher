using System.Globalization;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;
using YacaPluginSwitcher.UI;

namespace YacaPluginSwitcher;

public sealed class ProfessionalMainForm : Form
{
    private readonly YacaService _service;
    private readonly Panel _pageHost = new();
    private readonly Label _pageTitle = new();
    private readonly Label _statusLabel = new();
    private readonly Label _versionLabel = new();
    private readonly ComboBox _languageCombo = new();
    private readonly List<(string Key, Button Button)> _navButtons = [];
    private Control? _page;
    private TableLayoutPanel? _switchRows;
    private Label? _currentValue;
    private Label? _ts3Value;
    private Button? _ts3Close;
    private Label? _backupValue;
    private Label? _versionsValue;
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
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = new Font("Segoe UI", 10F);
        MinimumSize = new Size(1100, 720);
        Size = new Size(1440, 900);
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
        _navButtons.Clear();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 255));
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        shell.Controls.Add(BuildSidebar(), 0, 0);
        shell.Controls.Add(BuildContent(), 1, 0);
        ShowHome();
        ResumeLayout(true);
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(13, 14, 18),
            Padding = new Padding(18, 20, 18, 18),
            Margin = Padding.Empty
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = sidebar.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        sidebar.Controls.Add(root);

        var brand = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = sidebar.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        brand.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        brand.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        brand.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo,
            BackColor = sidebar.BackColor,
            Margin = Padding.Empty
        }, 0, 0);
        brand.Controls.Add(new Label
        {
            Text = "YACA",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 25F),
            ForeColor = Theme.Foreground,
            BackColor = sidebar.BackColor,
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 1);
        brand.Controls.Add(new Label
        {
            Text = "PLUGIN SWITCHER\nby ViP3R_76",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 10F),
            ForeColor = Theme.BrandGold,
            BackColor = sidebar.BackColor,
            TextAlign = ContentAlignment.TopCenter
        }, 0, 2);
        root.Controls.Add(brand, 0, 0);

        var navHost = new Panel { Dock = DockStyle.Fill, BackColor = sidebar.BackColor, AutoScroll = true, Padding = new Padding(0, 8, 0, 0) };
        DarkMode.ApplyScrollBarTheme(navHost);
        var nav = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 1,
            BackColor = sidebar.BackColor,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        navHost.Controls.Add(nav);
        AddNavButton(nav, "home", "⌂  Dashboard", ShowHome);
        AddNavButton(nav, "refresh", IsGerman ? "↻  Aktualisieren" : "↻  Refresh", () => RefreshActivePage(true));
        AddNavButton(nav, "switch", IsGerman ? "⇄  YACA wechseln" : "⇄  Switch YACA", ShowSwitchPage);
        AddNavButton(nav, "backup-create", IsGerman ? "＋  Backup erstellen" : "＋  Create Backup", CreateBackupFromDashboard);
        AddNavSeparator(nav);
        AddNavButton(nav, "backups", Texts.Backups, ShowBackups);
        AddNavButton(nav, "config", Texts.Config, ShowConfig);
        AddNavButton(nav, "info", Texts.About, ShowInfo);
        root.Controls.Add(navHost, 0, 1);

        root.Controls.Add(new Label
        {
            Text = IsGerman ? "☾  Dunkelmodus   ●" : "☾  Dark mode   ●",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10F),
            ForeColor = Theme.SecondaryForeground,
            BackColor = sidebar.BackColor,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        }, 0, 2);

        Theme.StyleComboBox(_languageCombo);
        _languageCombo.Items.Clear();
        _languageCombo.Items.Add(Texts.LanguageGerman);
        _languageCombo.Items.Add(Texts.LanguageEnglish);
        _languageCombo.SelectedIndex = IsGerman ? 0 : 1;
        _languageCombo.Dock = DockStyle.Fill;
        _languageCombo.Margin = new Padding(0, 6, 0, 6);
        _languageCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageCombo.SelectedIndexChanged -= LanguageChanged;
        _languageCombo.SelectedIndexChanged += LanguageChanged;
        root.Controls.Add(_languageCombo, 0, 3);

        var status = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = sidebar.BackColor,
            Margin = Padding.Empty,
            Padding = new Padding(0, 5, 0, 0)
        };
        _statusLabel.Text = $"●  {(IsGerman ? "Bereit" : "Ready")}";
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Theme.Success;
        _statusLabel.BackColor = sidebar.BackColor;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _versionLabel.Text = $"v{Application.ProductVersion}";
        _versionLabel.Dock = DockStyle.Fill;
        _versionLabel.ForeColor = Theme.Accent;
        _versionLabel.BackColor = sidebar.BackColor;
        _versionLabel.TextAlign = ContentAlignment.MiddleLeft;
        status.Controls.Add(_statusLabel, 0, 0);
        status.Controls.Add(_versionLabel, 0, 1);
        root.Controls.Add(status, 0, 4);
        return sidebar;
    }

    private void AddNavButton(TableLayoutPanel nav, string key, string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            Height = 46,
            Margin = new Padding(0, 2, 0, 2),
            Padding = new Padding(12, 0, 8, 0),
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = Theme.Foreground,
            Font = new Font("Segoe UI Semibold", 10F),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 28, 55);
        button.Click += (_, _) => action();
        nav.Controls.Add(button);
        _navButtons.Add((key, button));
    }

    private static void AddNavSeparator(TableLayoutPanel nav)
    {
        nav.Controls.Add(new Panel
        {
            Dock = DockStyle.Fill,
            Height = 1,
            BackColor = Color.FromArgb(55, 30, 75),
            Margin = new Padding(0, 10, 0, 10)
        });
    }

    private void SetActiveNav(string key)
    {
        foreach (var item in _navButtons)
        {
            var selected = item.Key == key;
            item.Button.BackColor = selected ? Color.FromArgb(45, 24, 72) : Color.Transparent;
            item.Button.ForeColor = selected ? Theme.BrandGold : Theme.Foreground;
            item.Button.FlatAppearance.BorderColor = selected ? Theme.Accent : Color.Transparent;
            item.Button.FlatAppearance.BorderSize = selected ? 1 : 0;
        }
    }

    private TableLayoutPanel BuildContent()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(28, 22, 28, 24),
            BackColor = Theme.Background,
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _pageTitle.Text = string.Empty;
        _pageTitle.Dock = DockStyle.Fill;
        _pageTitle.Font = new Font("Segoe UI Semibold", 18F);
        _pageTitle.ForeColor = Theme.Foreground;
        _pageTitle.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_pageTitle, 0, 0);

        _pageHost.Dock = DockStyle.Fill;
        _pageHost.BackColor = Theme.Background;
        root.Controls.Add(_pageHost, 0, 1);
        return root;
    }

    private void ShowHome()
    {
        _activePage = "home";
        _switchRows = null;
        _pageHost.Controls.Clear();
        _page = BuildDashboard();
        _page.Dock = DockStyle.Fill;
        _pageHost.Controls.Add(_page);
        _pageTitle.Text = string.Empty;
        SetActiveNav("home");
        RefreshHome();
    }

    private TableLayoutPanel BuildDashboard()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Theme.Background,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 31));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 36));

        var status = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 26));
        status.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        _currentValue = AddCurrentCard(status, 0, IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED");
        status.Controls.Add(BuildLogoCard(), 1, 0);
        (_ts3Value, _ts3Close) = AddTs3Card(status, 2);
        root.Controls.Add(status, 0, 0);

        var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        for (var i = 0; i < 3; i++)
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
        AddAction(actions, 0, "⇄", IsGerman ? "YACA WECHSELN" : "SWITCH YACA", IsGerman ? "Version auswählen und aktivieren" : "Select and activate a version", Theme.Accent, ShowSwitchPage, true);
        AddAction(actions, 1, "+", IsGerman ? "BACKUP ERSTELLEN" : "CREATE BACKUP", IsGerman ? "Aktuelle Version sichern" : "Save the current version", Theme.BrandGold, CreateBackupFromDashboard, false);
        AddAction(actions, 2, "⇩", "YACA UPDATER", IsGerman ? "Bald verfügbar · neueste DLL laden" : "Coming soon · download the latest DLL", Theme.Accent, ShowComingSoon, false);
        root.Controls.Add(actions, 0, 1);

        var lower = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = Theme.Background, Margin = Padding.Empty };
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        lower.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        _backupValue = AddInfoCard(lower, 0, IsGerman ? "LETZTES BACKUP" : "LATEST BACKUP");
        _versionsValue = AddInfoCard(lower, 1, IsGerman ? "VERFÜGBARE YACA-VERSIONEN" : "AVAILABLE YACA VERSIONS");
        root.Controls.Add(lower, 0, 2);
        return root;
    }

    private static Label AddCurrentCard(TableLayoutPanel host, int column, string title)
    {
        var card = Card(Theme.Accent);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(20) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = Theme.Accent, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 20F), ForeColor = Theme.Foreground, BackColor = Theme.Surface, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(value, 0, 1);
        card.Controls.Add(table);
        host.Controls.Add(card, column, 0);
        return value;
    }

    private Panel BuildLogoCard()
    {
        var card = Card(Theme.Accent);
        card.Controls.Add(new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            Image = Branding.Logo,
            BackColor = Theme.Surface,
            Margin = Padding.Empty,
            Padding = new Padding(14)
        });
        return card;
    }

    private (Label Value, Button Close) AddTs3Card(TableLayoutPanel host, int column)
    {
        var card = Card(Theme.BrandGold);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Surface, Padding = new Padding(20) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        table.Controls.Add(new Label { Text = "TEAMSpeak 3 STATUS", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = Theme.BrandGold, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 19F), ForeColor = Theme.BrandGold, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft };
        table.Controls.Add(value, 0, 1);
        var close = new Button { Text = "", Dock = DockStyle.Fill, Visible = false, Margin = new Padding(0, 4, 0, 0) };
        Theme.StyleButton(close);
        close.FlatAppearance.BorderColor = Theme.BrandGold;
        close.Click += (_, _) => CloseTeamSpeak();
        table.Controls.Add(close, 0, 2);
        card.Controls.Add(table);
        host.Controls.Add(card, column, 0);
        return (value, close);
    }

    private static void AddAction(TableLayoutPanel host, int column, string icon, string title, string subtitle, Color accent, Action action, bool primary)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(40, 25, 58) : Theme.Surface,
            ForeColor = Theme.Foreground,
            Font = new Font("Segoe UI Semibold", primary ? 12F : 11F),
            Text = $"{icon}  {title}\n{subtitle}",
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = accent;
        button.FlatAppearance.BorderSize = primary ? 2 : 1;
        button.FlatAppearance.MouseOverBackColor = Theme.ControlHover;
        button.Click += (_, _) => action();
        host.Controls.Add(button, column, 0);
    }

    private static Label AddInfoCard(TableLayoutPanel host, int column, string title)
    {
        var card = Card(Theme.Control);
        var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Surface, Padding = new Padding(20) };
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        table.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 10F), ForeColor = Theme.Accent, BackColor = Theme.Surface, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var value = new Label { Text = "—", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), ForeColor = Theme.Foreground, BackColor = Theme.Surface, AutoEllipsis = true, TextAlign = ContentAlignment.TopLeft };
        table.Controls.Add(value, 0, 1);
        card.Controls.Add(table);
        host.Controls.Add(card, column, 0);
        return value;
    }

    private static Panel Card(Color accent)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Margin = new Padding(6), Padding = new Padding(1) };
        panel.Paint += (_, e) => { using var pen = new Pen(accent, 1); e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, panel.Width - 1), Math.Max(0, panel.Height - 1)); };
        return panel;
    }

    private void ShowSwitchPage()
    {
        _activePage = "switch";
        _pageHost.Controls.Clear();
        _switchRows = null;
        _page = BuildSwitchPage();
        _page.Dock = DockStyle.Fill;
        _pageHost.Controls.Add(_page);
        _pageTitle.Text = IsGerman ? "YACA wechseln" : "Switch YACA";
        SetActiveNav("switch");
        RefreshSwitchPage(false);
    }

    private TableLayoutPanel BuildSwitchPage()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Background, Margin = Padding.Empty };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = IsGerman ? "Verfügbare valide YACA-DLLs" : "Available valid YACA DLLs", Dock = DockStyle.Fill, Font = new Font("Segoe UI Semibold", 11F), ForeColor = Theme.SecondaryForeground, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        var host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(14), AutoScroll = true };
        DarkMode.ApplyScrollBarTheme(host);
        _switchRows = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 0, BackColor = Theme.Surface };
        host.Controls.Add(_switchRows);
        root.Controls.Add(host, 0, 1);
        return root;
    }

    private void ShowBackups() => ShowEmbeddedForm(new BackupForm(_service), Texts.Backups, "backups");
    private void ShowConfig() => ShowEmbeddedForm(new ConfigForm(_service), Texts.ConfigTitle, "config");

    private void ShowEmbeddedForm(Form form, string title, string navKey)
    {
        _activePage = "embedded";
        _pageHost.Controls.Clear();
        _page = form;
        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.ShowInTaskbar = false;
        form.StartPosition = FormStartPosition.Manual;
        form.MinimumSize = Size.Empty;
        form.MaximumSize = Size.Empty;
        form.Dock = DockStyle.Fill;
        form.AutoScroll = true;
        form.Margin = Padding.Empty;
        form.Parent = _pageHost;
        form.Show();
        form.BringToFront();
        _pageTitle.Text = title;
        SetActiveNav(navKey);
        form.FormClosed += (_, _) => ShowHome();
    }

    private void ShowInfo()
    {
        _activePage = "info";
        _pageHost.Controls.Clear();
        _page = new InfoPage(_service.Settings.Language);
        _page.Dock = DockStyle.Fill;
        _pageHost.Controls.Add(_page);
        _pageTitle.Text = Texts.AboutTitle;
        SetActiveNav("info");
    }

    private void ShowComingSoon()
    {
        MessageBox.Show(this, IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version.", "YACA Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LanguageChanged(object? sender, EventArgs e)
    {
        if (_languageCombo.SelectedIndex < 0)
            return;
        var language = _languageCombo.SelectedIndex == 0 ? Localization.German : Localization.English;
        if (string.Equals(_service.Settings.Language, language, StringComparison.OrdinalIgnoreCase))
            return;
        _service.Settings.Language = language;
        _service.Settings.Save();
        RebuildForLanguage();
    }

    private void RebuildForLanguage()
    {
        var active = _activePage;
        BuildShell();
        switch (active)
        {
            case "switch":
                ShowSwitchPage();
                break;
            case "info":
                ShowInfo();
                break;
            default:
                ShowHome();
                break;
        }
    }

    private void RefreshActivePage(bool announce)
    {
        switch (_activePage)
        {
            case "home":
                RefreshHome(announce);
                break;
            case "switch":
                RefreshSwitchPage(announce);
                break;
            default:
                ShowHome();
                break;
        }
    }

    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home")
            return;
        try
        {
            var text = Texts;
            var current = _service.DetectCurrent();
            var plugins = GetDistinctPlugins();
            var notice = announce ? GetNewPluginNotice(plugins, text) : null;
            if (!_pluginBaselineInitialized)
                SetPluginBaseline(plugins);

            _currentValue!.Text = current?.DisplayName ?? (File.Exists(_service.TargetFile) ? text.UnknownInvalid : text.NotInstalled);
            _currentValue.ForeColor = current is null ? Theme.Warning : Theme.Success;

            var running = TeamSpeakDetector.IsRunning();
            _ts3Value!.Text = running ? (IsGerman ? "GESTARTET" : "RUNNING") : (IsGerman ? "NICHT GESTARTET" : "NOT RUNNING");
            _ts3Value.ForeColor = running ? Theme.Error : Theme.Success;
            _ts3Close!.Visible = running;
            _ts3Close.Text = text.CloseTeamspeak;

            var backup = _service.Backups.ListBackups().FirstOrDefault();
            _backupValue!.Text = backup is null ? text.NoBackups : $"{backup.Timestamp:dd.MM.yyyy HH:mm:ss}\n{backup.DisplayName}\n{backup.FileSize:N0} Bytes";
            _versionsValue!.Text = plugins.Count == 0 ? text.NoPlugins : string.Join(Environment.NewLine, plugins.Select(p => p.DisplayName));

            _statusLabel.Text = $"●  {(string.IsNullOrWhiteSpace(notice) ? (running ? text.TeamspeakRunning : text.TeamspeakStopped) : notice)}";
            _statusLabel.ForeColor = Theme.Success;
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Dashboard refresh failed: {ex}");
            _statusLabel.Text = $"●  {Texts.ErrorUnexpected}";
            _statusLabel.ForeColor = Theme.Error;
        }
    }

    private List<YacaPluginInfo> GetDistinctPlugins()
    {
        var result = new List<YacaPluginInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var plugin in _service.ScanPlugins())
        {
            if (seen.Add(GetPluginKey(plugin)))
                result.Add(plugin);
        }
        return result;
    }

    private void RefreshSwitchPage(bool announce)
    {
        if (_switchRows is null)
            return;
        try
        {
            var text = Texts;
            var current = _service.DetectCurrent();
            var plugins = GetDistinctPlugins();
            var notice = announce ? GetNewPluginNotice(plugins, text) : null;
            if (!_pluginBaselineInitialized)
                SetPluginBaseline(plugins);

            _switchRows.SuspendLayout();
            _switchRows.Controls.Clear();
            _switchRows.RowStyles.Clear();
            _switchRows.RowCount = 0;
            if (!string.IsNullOrWhiteSpace(notice))
                AddSwitchLabel(notice, Theme.Success);
            if (plugins.Count == 0)
                AddSwitchLabel(text.NoPlugins, Theme.Warning);

            foreach (var plugin in plugins)
            {
                var active = current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase);
                var button = new Button
                {
                    Text = active ? $"{plugin.DisplayName}   —   {text.Active.TrimEnd(':')}" : plugin.DisplayName,
                    Height = 50,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 0, 0, 8),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control,
                    ForeColor = active ? Theme.Success : Theme.Foreground
                };
                Theme.StyleButton(button);
                button.BackColor = active ? Color.FromArgb(38, 52, 44) : Theme.Control;
                button.ForeColor = active ? Theme.Success : Theme.Foreground;
                button.Click += (_, _) => Activate(plugin);
                _switchRows.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
                _switchRows.Controls.Add(button, 0, _switchRows.RowCount++);
            }
            _switchRows.ResumeLayout(true);
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Switch page refresh failed: {ex}");
            ShowError(Texts.ErrorUnexpected);
        }
    }

    private void AddSwitchLabel(string text, Color color)
    {
        _switchRows!.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _switchRows.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = color, BackColor = Theme.Surface, Margin = new Padding(0, 0, 0, 10) }, 0, _switchRows.RowCount++);
    }

    private void Activate(YacaPluginInfo plugin)
    {
        var text = Texts;
        var current = _service.DetectCurrent();
        if (current is not null && string.Equals(current.Sha256, plugin.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, $"{plugin.DisplayName} {text.AlreadyActiveMessage}", text.AlreadyActiveTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning() && MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            Cursor = Cursors.WaitCursor;
            _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException)
        {
            _service.Logger.Error($"YACA switch failed: {ex}");
            ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected));
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshSwitchPage(false);
        }
    }

    private void CreateBackupFromDashboard()
    {
        var text = Texts;
        if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning && MessageBox.Show(this, text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
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
            _service.Backups.Trim(_service.Settings.MaxBackups);
            RefreshHome();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _service.Logger.Error($"Dashboard backup failed: {ex}");
            ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected));
        }
    }

    private void CloseTeamSpeak()
    {
        var text = Texts;
        if (!TeamSpeakDetector.IsRunning())
        {
            RefreshHome();
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
            RefreshHome();
        }
    }

    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins, UiText text)
    {
        var keys = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!_pluginBaselineInitialized)
        {
            _knownPlugins = keys;
            _pluginBaselineInitialized = true;
            return null;
        }
        var added = plugins.Where(p => !_knownPlugins.Contains(GetPluginKey(p))).ToList();
        _knownPlugins = keys;
        return added.Count == 0 ? null : string.Format(CultureInfo.CurrentCulture, text.NewValidPluginFound, string.Join(", ", added.Select(p => p.DisplayName)));
    }

    private static string GetPluginKey(YacaPluginInfo plugin) => $"{plugin.FilePath}|{plugin.Sha256}";

    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins)
    {
        _knownPlugins = plugins.Select(GetPluginKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _pluginBaselineInitialized = true;
    }

    private void ShowError(string message) => MessageBox.Show(this, message, Texts.ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
}
