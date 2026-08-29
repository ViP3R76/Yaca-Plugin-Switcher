using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow : Window
{
    private readonly YacaService _service;
    private readonly List<(string Key, Button Button)> _navButtons = [];
    private readonly List<YacaPluginInfo> _plugins = [];
    private string _activePage = "home";
    private TextBlock? _currentValue, _currentDetails, _tsStatus, _tsDescription, _backupSummary, _pageStatus;
    private Button? _tsClose;
    private StackPanel? _versionList;
    private Border? _currentCard, _backupCard, _pageStatusBorder;
    private HashSet<string> _knownPlugins = new(StringComparer.OrdinalIgnoreCase);
    private bool _pluginBaselineInitialized;
    private System.Windows.Threading.DispatcherTimer? _flashTimer;
    private UiText Texts => Localization.Get(_service.Settings.Language);
    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;

    public MainWindow(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        InitializeComponent();
        VersionText.Text = "v1.1.0";
        BuildNavigation();
        LoadLanguageSelector();
        ShowHome();
    }

    private void BuildNavigation()
    {
        NavPanel.Children.Clear();
        _navButtons.Clear();
        AddNav("home", "⌂", "Dashboard", ShowHome);
        AddNav("refresh", "↻", IsGerman ? "Aktualisieren" : "Refresh", () => RefreshActivePage(true));
        AddNav("switch", "⇄", IsGerman ? "YACA wechseln" : "Switch YACA", ShowSwitchPage);
        AddNav("updater", "☁", "YACA Updater", () => ShowError(IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version."));
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("backup-create", "＋", IsGerman ? "Backup erstellen" : "Create Backup", CreateBackupFromDashboard);
        AddNav("backups", "▣", IsGerman ? "Backup verwalten" : "Manage Backups", ShowBackups);
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("info", "ⓘ", IsGerman ? "Info & Links" : "Info & Links", ShowInfo);
    }

    private void AddNav(string key, string icon, string text, Action action)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(new TextBlock { Text = icon, FontSize = 23, Width = 34, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = text, FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
        var button = new Button { Style = (Style)FindResource("NavButtonStyle"), Height = 46, Tag = key, Content = content };
        button.Click += (_, _) => action();
        NavPanel.Children.Add(button);
        _navButtons.Add((key, button));
    }

    private void SetActiveNav(string key)
    {
        foreach (var item in _navButtons)
        {
            var selected = item.Key.Equals(key, StringComparison.OrdinalIgnoreCase);
            item.Button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
            item.Button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
            item.Button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
            item.Button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);
        }
    }

    private void LoadLanguageSelector()
    {
        LanguageCombo.Items.Clear();
        LanguageCombo.Items.Add(Texts.LanguageGerman);
        LanguageCombo.Items.Add(Texts.LanguageEnglish);
        LanguageCombo.SelectedIndex = IsGerman ? 0 : 1;
    }

    private void ShowHome()
    {
        _activePage = "home";
        _pageStatus = null;
        _pageStatusBorder = null;
        PageHost.Content = RenderDashboard();
        SetActiveNav("home");
        RefreshHome();
    }

    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home") return;
        try
        {
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());
            var notice = announce ? GetNewPluginNotice(_plugins) : null;
            if (!_pluginBaselineInitialized) SetPluginBaseline(_plugins);
            var current = _service.DetectCurrent();
            UpdateCurrentInstalled(current);
            if (announce) FlashElement(_currentCard);
            var running = TeamSpeakDetector.IsRunning();
            _tsStatus!.Text = running ? (IsGerman ? "GESTARTET" : "RUNNING") : (IsGerman ? "NICHT GESTARTET" : "NOT RUNNING");
            _tsStatus.Foreground = running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush");
            _tsDescription!.Text = running ? (IsGerman ? "TeamSpeak 3 ist aktiv. Für einen sicheren Wechsel bitte zuerst schließen." : "TeamSpeak 3 is active. Close it before switching.") : (IsGerman ? "TeamSpeak 3 ist nicht aktiv.\nWechsel jederzeit möglich." : "TeamSpeak 3 is not active.\nSwitching is ready.");
            _tsClose!.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
            var backup = _service.Backups.ListBackups().FirstOrDefault();
            _backupSummary!.Text = backup is null ? Texts.NoBackups : $"{backup.Timestamp:dd.MM.yyyy HH:mm}\n{backup.DisplayName}  •  {backup.FileSize / 1024d / 1024d:0.00} MB\nDatei  {backup.FileName}";
            RenderVersionList(current);
            if (_pageStatus is not null) _pageStatus.Text = string.IsNullOrWhiteSpace(notice) ? (running ? Texts.TeamspeakRunning : Texts.TeamspeakStopped) : notice;
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Dashboard refresh failed: {ex}");
            ShowError(Texts.ErrorUnexpected);
        }
    }

    private void UpdateCurrentInstalled(YacaPluginInfo? current)
    {
        _currentValue!.Text = current?.Version?.ToString() ?? (File.Exists(_service.TargetFile) ? Texts.UnknownInvalid : Texts.NotInstalled);
        _currentValue.Foreground = current is null ? (Brush)FindResource("WarningBrush") : (Brush)FindResource("ForegroundBrush");
        if (_currentDetails is not null)
            _currentDetails.Text = current is null ? string.Empty : $"Build: {current.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"}\nGröße: {current.FileSize / 1024d / 1024d:0.00} MB\nSHA-256: {current.Sha256}\nDatei: {System.IO.Path.GetFileName(current.FilePath)}";
    }

    private List<YacaPluginInfo> GetDistinctPlugins()
    {
        var result = new List<YacaPluginInfo>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in _service.ScanPlugins()) if (seen.Add($"{p.FilePath}|{p.Sha256}")) result.Add(p);
        return result;
    }

    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins)
    {
        var keys = plugins.Select(p => $"{p.FilePath}|{p.Sha256}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!_pluginBaselineInitialized) { _knownPlugins = keys; _pluginBaselineInitialized = true; return null; }
        var added = plugins.Where(p => !_knownPlugins.Contains($"{p.FilePath}|{p.Sha256}")).ToList();
        _knownPlugins = keys;
        return added.Count == 0 ? null : string.Format(CultureInfo.CurrentCulture, Texts.NewValidPluginFound, string.Join(", ", added.Select(p => p.DisplayName)));
    }

    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins)
    {
        _knownPlugins = plugins.Select(p => $"{p.FilePath}|{p.Sha256}").ToHashSet(StringComparer.OrdinalIgnoreCase);
        _pluginBaselineInitialized = true;
    }

    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch"; SetActiveNav("switch");
        var root = new Grid(); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) }); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
        root.Children.Add(new TextBlock { Text = IsGerman ? "YACA wechseln" : "Switch YACA", FontSize = 24, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        var list = new StackPanel(); var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }; Grid.SetRow(scroll, 1); root.Children.Add(scroll);
        var current = _service.DetectCurrent();
        foreach (var p in GetDistinctPlugins()) { var active = current?.Sha256.Equals(p.Sha256, StringComparison.OrdinalIgnoreCase) == true; var b = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("AccentBrush"), Margin = new Thickness(6), Height = 58, HorizontalContentAlignment = HorizontalAlignment.Left, Content = new TextBlock { Text = active ? $"{p.DisplayName}   —   {Texts.Active.TrimEnd(':')}" : p.DisplayName, FontSize = 15, Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush") } }; b.Click += (_, _) => Activate(p); list.Children.Add(b); }
        _pageStatusBorder = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("BorderBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Margin = new Thickness(6, 6, 6, 0), Padding = new Thickness(14, 0, 14, 0) };
        _pageStatus = new TextBlock { Text = status ?? (IsGerman ? "Bereit." : "Ready."), FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center }; _pageStatusBorder.Child = _pageStatus; Grid.SetRow(_pageStatusBorder, 2); root.Children.Add(_pageStatusBorder); PageHost.Content = root;
    }

    private void Activate(YacaPluginInfo plugin)
    {
        var text = Texts; var current = _service.DetectCurrent(); if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true) { ShowPageStatus(text.AlreadyActiveMessage, false); return; }
        if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning() && MessageBox.Show(text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Mouse.OverrideCursor = Cursors.Wait; _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups); ShowSwitchPage($"{plugin.DisplayName} {text.ActivatedMessage}"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException) { _service.Logger.Error($"YACA switch failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); }
        finally { Mouse.OverrideCursor = null; }
    }

    private void CreateBackupFromDashboard()
    {
        var text = Texts; if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning && MessageBox.Show(text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { var current = _service.DetectCurrent(); if (current is null) { ShowError(text.NotInstalled); return; } if (_service.Backups.CreateBackup(_service.TargetFile, current) is null) { ShowError(text.ErrorUnexpected); return; } _service.Backups.Trim(_service.Settings.MaxBackups); RefreshHome(); FlashElement(_backupCard); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException) { _service.Logger.Error($"Dashboard backup failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); }
    }

    private void CloseTeamSpeak()
    {
        var text = Texts; if (!TeamSpeakDetector.IsRunning()) { RefreshHome(); return; } if (MessageBox.Show(text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Mouse.OverrideCursor = Cursors.Wait; if (!TeamSpeakDetector.TryClose(TimeSpan.FromSeconds(5))) MessageBox.Show(text.CloseTeamspeakFailed, text.TeamspeakRunningTitle, MessageBoxButton.OK, MessageBoxImage.Error); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed)); }
        finally { Mouse.OverrideCursor = null; RefreshHome(); }
    }

    private void ShowBackups() { _activePage = "backups"; SetActiveNav("backups"); PageHost.Content = new BackupView(_service, this); }
    private void ShowConfig() { _activePage = "config"; SetActiveNav("config"); PageHost.Content = new ConfigView(_service, this); }
    private void ShowInfo() { _activePage = "info"; SetActiveNav("info"); PageHost.Content = new InfoView(_service.Settings.Language); }
    private void ShowComingSoon() { ShowError(IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version."); }
    internal void ReturnHome() => ShowHome();

    private void RefreshActivePage(bool announce)
    {
        if (_activePage == "home") RefreshHome(announce);
        else if (_activePage == "switch") ShowSwitchPage();
        else if (_activePage == "backups") PageHost.Content = new BackupView(_service, this);
        else if (_activePage == "config") PageHost.Content = new ConfigView(_service, this);
    }

    private void ShowPageStatus(string message, bool success)
    {
        if (_pageStatus is null) { ShowSwitchPage(message); return; }
        _pageStatus.Text = message; _pageStatus.Foreground = (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush"); _pageStatusBorder!.BorderBrush = (Brush)FindResource(success ? "SuccessBrush" : "BorderBrush");
    }

    private void FlashElement(Border? element)
    {
        if (element is null) return; _flashTimer?.Stop(); var original = element.BorderBrush; element.BorderBrush = (Brush)FindResource("GoldBrush"); _flashTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) }; _flashTimer.Tick += (_, _) => { element.BorderBrush = original; _flashTimer!.Stop(); }; _flashTimer.Start();
    }

    private void ShowError(string message)
    {
        if (_activePage != "home" && _pageStatus is not null) { ShowPageStatus(message, false); return; }
        MessageBox.Show(message, Texts.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
