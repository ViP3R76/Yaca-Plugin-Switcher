using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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
    private TextBlock? _currentValue, _currentDetails, _tsStatus, _tsDescription, _backupSummary;
    private Button? _tsClose;
    private StackPanel? _versionList;
    private Border? _currentCard, _backupCard;
    private HashSet<string> _knownPlugins = new(StringComparer.OrdinalIgnoreCase);
    private bool _pluginBaselineInitialized;
    private System.Windows.Threading.DispatcherTimer? _flashTimer;
    private UiText Texts => Localization.Get(_service.Settings.Language);
    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;

    public MainWindow(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service)); InitializeComponent(); GlobalFooterVersionText.Text = "v1.1.0"; BuildNavigation(); LoadLanguageSelector(); ShowHome();
    }

    private void BuildNavigation()
    {
        NavPanel.Children.Clear(); _navButtons.Clear();
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 0, 0, 8), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("home", "home", "Dashboard", ShowHome);
        AddNav("refresh", "refresh", IsGerman ? "Aktualisieren" : "Refresh", () => RefreshActivePage(true));
        AddNav("switch", "switch", IsGerman ? "YACA wechseln" : "Switch YACA", () => ShowSwitchPage());
        AddNav("updater", "updater", "YACA Updater", () => ShowComingSoon());
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("backup-create", "backup", IsGerman ? "Backup erstellen" : "Create Backup", () => CreateBackupFromDashboard());
        AddNav("backups", "backups", IsGerman ? "Backup verwalten" : "Manage Backups", ShowBackups);
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("info", "info", "Info & Links", ShowInfo);
        if (ExitNavText is not null) ExitNavText.Text = IsGerman ? "Beenden" : "Exit";
    }

    private void AddNav(string key, string iconKey, string text, Action action)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center }; System.Windows.Shapes.Path? icon = null;
        if (DashboardIconData.TryGetValue(iconKey, out var data)) { icon = CreateIcon(data, (Brush)FindResource("ForegroundBrush"), 30, 30, 0); content.Children.Add(icon); }
        content.Children.Add(new TextBlock { Text = text, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) });
        var button = new Button { Style = (Style)FindResource("NavButtonStyle"), Height = 46, Tag = key, Content = content };
        button.MouseEnter += (_, _) => { button.Background = (Brush)FindResource("NavSelectedBrush"); button.Foreground = (Brush)FindResource("GoldBrush"); button.BorderBrush = (Brush)FindResource("GoldBrush"); if (icon is not null) icon.Fill = (Brush)FindResource("GoldBrush"); };
        button.MouseLeave += (_, _) => { var selected = string.Equals(_activePage, key, StringComparison.OrdinalIgnoreCase); button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent; button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"); button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent; if (icon is not null) icon.Fill = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"); };
        button.Click += (_, _) => action(); NavPanel.Children.Add(button); _navButtons.Add((key, button));
    }

    private void SetActiveNav(string key)
    {
        _activePage = key;
        foreach (var item in _navButtons)
        {
            var selected = item.Key.Equals(key, StringComparison.OrdinalIgnoreCase); item.Button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent; item.Button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"); item.Button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent; item.Button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);
            if (item.Button.Content is StackPanel panel && panel.Children.OfType<System.Windows.Shapes.Path>().FirstOrDefault() is { } icon) icon.Fill = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
        }
    }

    private void LoadLanguageSelector() { LanguageCombo.Items.Clear(); LanguageCombo.Items.Add(Texts.LanguageGerman); LanguageCombo.Items.Add(Texts.LanguageEnglish); LanguageCombo.SelectedIndex = IsGerman ? 0 : 1; if (ExitNavText is not null) ExitNavText.Text = IsGerman ? "Beenden" : "Exit"; }
    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!IsInitialized || LanguageCombo.SelectedIndex < 0) return; var language = LanguageCombo.SelectedIndex == 0 ? Localization.German : Localization.English; if (string.Equals(Localization.Normalize(_service.Settings.Language), language, StringComparison.OrdinalIgnoreCase)) return; _service.Settings.Language = language; _service.Settings.Save(); BuildNavigation(); LoadLanguageSelector(); ShowCurrentPageAfterLanguageChange(); }
    private void ShowCurrentPageAfterLanguageChange() { switch (_activePage) { case "switch": ShowSwitchPage(); break; case "backups": ShowBackups(); break; case "config": ShowConfig(); break; case "info": ShowInfo(); break; default: ShowHome(); break; } }
    private void SetGlobalStatus(string message, bool success = false) { GlobalFooterStatusText.Text = message; GlobalFooterStatusText.Foreground = (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush"); }

    private void ShowHome() { _activePage = "home"; PageHost.Content = RenderDashboard(); SetActiveNav("home"); SetGlobalStatus(IsGerman ? "Bereit." : "Ready."); RefreshHome(); }

    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home") return;
        try
        {
            _plugins.Clear(); _plugins.AddRange(GetDistinctPlugins()); var notice = announce ? GetNewPluginNotice(_plugins) : null; if (!_pluginBaselineInitialized) SetPluginBaseline(_plugins); var current = _service.DetectCurrent(); UpdateCurrentInstalled(current); if (announce) FlashElement(_currentCard);
            var running = TeamSpeakDetector.IsRunning(); _tsStatus!.Text = running ? (IsGerman ? "GESTARTET" : "RUNNING") : (IsGerman ? "NICHT GESTARTET" : "NOT RUNNING"); _tsStatus.Foreground = running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"); _tsDescription!.Text = running ? (IsGerman ? "TeamSpeak 3 ist aktiv!\nFür einen sicheren Wechsel bitte zuerst schliessen." : "TeamSpeak 3 is active!\nFor a safe switch, please close it first.") : (IsGerman ? "TeamSpeak 3 ist nicht aktiv.\nWechsel jederzeit möglich." : "TeamSpeak 3 is not active.\nSwitching is ready."); _tsClose!.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
            UpdateBackupSummary(_service.Backups.ListBackups().FirstOrDefault()); RenderVersionList(current); SetGlobalStatus(string.IsNullOrWhiteSpace(notice) ? (running ? Texts.TeamspeakRunning : Texts.TeamspeakStopped) : notice);
        }
        catch (Exception ex) { _service.Logger.Error($"Dashboard refresh failed: {ex}"); ShowError(Texts.ErrorUnexpected); }
    }

    private void UpdateCurrentInstalled(YacaPluginInfo? current) { _currentValue!.Text = current?.Version?.ToString() ?? (File.Exists(_service.TargetFile) ? Texts.UnknownInvalid : Texts.NotInstalled); _currentValue.Foreground = current is null ? (Brush)FindResource("WarningBrush") : (Brush)FindResource("ForegroundBrush"); if (_currentDetails is not null) _currentDetails.Text = current is null ? string.Empty : $"Build: {current.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"}\nGröße: {current.FileSize / 1024d / 1024d:0.00} MB\nSHA-256: {current.Sha256}\nDatei: {System.IO.Path.GetFileName(current.FilePath)}"; }
    private List<YacaPluginInfo> GetDistinctPlugins() { var result = new List<YacaPluginInfo>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (var p in _service.ScanPlugins()) if (seen.Add($"{p.FilePath}|{p.Sha256}")) result.Add(p); return result; }
    private string? GetNewPluginNotice(IReadOnlyList<YacaPluginInfo> plugins) { var keys = plugins.Select(p => $"{p.FilePath}|{p.Sha256}").ToHashSet(StringComparer.OrdinalIgnoreCase); if (!_pluginBaselineInitialized) { _knownPlugins = keys; _pluginBaselineInitialized = true; return null; } var added = plugins.Where(p => !_knownPlugins.Contains($"{p.FilePath}|{p.Sha256}")).ToList(); _knownPlugins = keys; return added.Count == 0 ? null : string.Format(CultureInfo.CurrentCulture, Texts.NewValidPluginFound, string.Join(", ", added.Select(p => p.DisplayName))); }
    private void SetPluginBaseline(IReadOnlyList<YacaPluginInfo> plugins) { _knownPlugins = plugins.Select(p => $"{p.FilePath}|{p.Sha256}").ToHashSet(StringComparer.OrdinalIgnoreCase); _pluginBaselineInitialized = true; }

    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch"; SetActiveNav("switch"); var root = new Grid { Margin = new Thickness(0, 4, 0, 0) }; root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("AccentBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(6) }; var leftPanel = new Grid(); leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); leftPanel.Children.Add(CreateDashboardHeader("switch", IsGerman ? "VERFÜGBARE YACA VERSIONEN" : "AVAILABLE YACA VERSIONS", (Brush)FindResource("AccentBrush")));
        var list = new StackPanel { Margin = new Thickness(6, 10, 6, 6) }; var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Background = (Brush)FindResource("SurfaceBrush") }; Grid.SetRow(scroll, 1); leftPanel.Children.Add(scroll); left.Child = leftPanel; Grid.SetColumn(left, 0); root.Children.Add(left);
        var right = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("GoldBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(6) }; var rightPanel = new Grid(); rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); rightPanel.Children.Add(CreateDashboardHeader("updater", IsGerman ? "VERFÜGBARE YACA UPDATE-/DOWNLOADS" : "AVAILABLE YACA UPDATES/DOWNLOADS", (Brush)FindResource("GoldBrush"))); var updaterInfo = new StackPanel { Margin = new Thickness(6, 18, 6, 6), VerticalAlignment = VerticalAlignment.Top }; updaterInfo.Children.Add(new TextBlock { Text = IsGerman ? "Hier werden verfügbare YACA Updates und Downloads angezeigt." : "Available YACA updates and downloads will be shown here.", FontSize = 15, Foreground = (Brush)FindResource("SecondaryBrush"), TextWrapping = TextWrapping.Wrap }); Grid.SetRow(updaterInfo, 1); rightPanel.Children.Add(updaterInfo); right.Child = rightPanel; Grid.SetColumn(right, 1); root.Children.Add(right);
        var current = _service.DetectCurrent(); foreach (var p in GetDistinctPlugins()) { var active = current?.Sha256.Equals(p.Sha256, StringComparison.OrdinalIgnoreCase) == true; var b = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("AccentBrush"), Margin = new Thickness(0, 2, 0, 2), Height = 58, HorizontalContentAlignment = HorizontalAlignment.Left, Content = new TextBlock { Text = active ? $"{p.DisplayName}   —   {Texts.Active.TrimEnd(':')}" : p.DisplayName, FontSize = 15, Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush") } }; b.Click += (_, _) => Activate(p); list.Children.Add(b); }
        PageHost.Content = root; SetGlobalStatus(status ?? (IsGerman ? "Bereit." : "Ready."));
    }

    private void Activate(YacaPluginInfo plugin) { var text = Texts; var current = _service.DetectCurrent(); if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true) { SetGlobalStatus(text.AlreadyActiveMessage); return; } if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning() && MessageBox.Show(text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; try { Mouse.OverrideCursor = Cursors.Wait; _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups); ShowSwitchPage($"{plugin.DisplayName} {text.ActivatedMessage}"); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException) { _service.Logger.Error($"YACA switch failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); } finally { Mouse.OverrideCursor = null; } }
    private void CreateBackupFromDashboard() { var text = Texts; if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning && MessageBox.Show(text.TeamspeakRunningMessage, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; try { var current = _service.DetectCurrent(); if (current is null) { ShowError(text.NotInstalled); return; } if (_service.Backups.CreateBackup(_service.TargetFile, current, automatic: false) is null) { ShowError(text.ErrorUnexpected); return; } _service.Backups.Trim(_service.Settings.MaxBackups); RefreshHome(); FlashElement(_backupCard); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException) { _service.Logger.Error($"Dashboard backup failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); } }

    private void CloseTeamSpeak()
    {
        var text = Texts; if (!TeamSpeakDetector.IsRunning()) { RefreshHome(); return; } if (MessageBox.Show(text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try { Mouse.OverrideCursor = Cursors.Wait; if (!TeamSpeakDetector.TryCloseWithElevation(TimeSpan.FromSeconds(10))) ShowError(text.CloseTeamspeakFailed); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed)); }
        finally { Mouse.OverrideCursor = null; RefreshHome(); }
    }

    private void ShowBackups() { _activePage = "backups"; SetActiveNav("backups"); PageHost.Content = new BackupView(_service, this); SetGlobalStatus(IsGerman ? "Backupverwaltung geöffnet." : "Backup management opened."); }
    private void ShowConfig() { _activePage = "config"; SetActiveNav("config"); PageHost.Content = new ConfigView(_service, this); SetGlobalStatus(IsGerman ? "Konfiguration geöffnet." : "Configuration opened."); }
    private void ShowInfo() { _activePage = "info"; SetActiveNav("info"); PageHost.Content = new InfoView(_service.Settings.Language); SetGlobalStatus(IsGerman ? "Info & Links geöffnet." : "Info & Links opened."); }
    private void ShowComingSoon() => SetGlobalStatus(IsGerman ? "Der YACA Updater wird in einer späteren Version verfügbar sein." : "The YACA Updater will be available in a future version.");
    internal void ReturnHome() => ShowHome();
    private void RefreshActivePage(bool announce) { if (_activePage == "home") RefreshHome(announce); else if (_activePage == "switch") ShowSwitchPage(); else if (_activePage == "backups") PageHost.Content = new BackupView(_service, this); else if (_activePage == "config") PageHost.Content = new ConfigView(_service, this); }
    private void ShowError(string message) => SetGlobalStatus(message);
    private void FlashElement(Border? element) { if (element is null) return; _flashTimer?.Stop(); var original = element.BorderBrush; element.BorderBrush = (Brush)FindResource("GoldBrush"); _flashTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) }; _flashTimer.Tick += (_, _) => { element.BorderBrush = original; _flashTimer!.Stop(); }; _flashTimer.Start(); }
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; } if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}