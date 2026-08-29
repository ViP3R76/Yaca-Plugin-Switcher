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
    private readonly YacaUpdaterService _updater;
    private readonly List<(string Key, Button Button)> _navButtons = [];
    private readonly List<YacaPluginInfo> _plugins = [];
    private string _activePage = "home";
    private TextBlock? _currentValue, _currentDetails, _tsStatus, _tsDescription, _backupSummary;
    private Button? _tsClose;
    private StackPanel? _versionList, _downloadedFilesPanel;
    private Border? _currentCard, _backupCard;
    private ProgressBar? _updaterProgress;
    private TextBlock? _updaterStatus, _updaterVersion, _updaterSize;
    private CancellationTokenSource? _updaterCts;
    private UiText Texts => Localization.Get(_service.Settings.Language);
    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;
    private bool _switchSortDescending = true;

    public MainWindow(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _updater = new YacaUpdaterService(_service);
        InitializeComponent(); GlobalFooterVersionText.Text = "v1.1.0"; BuildNavigation(); LoadLanguageSelector(); ShowHome();
    }

    private void BuildNavigation()
    {
        NavPanel.Children.Clear(); _navButtons.Clear();
        AddNav("home", DashboardIconRegistry.IconAssetDashboard, "Dashboard", ShowHome);
        AddNav("refresh", DashboardIconRegistry.IconAssetRefresh, IsGerman ? "Aktualisieren" : "Refresh", () => { SetGlobalStatus(IsGerman ? "Aktualisierung wird ausgeführt …" : "Refreshing …"); RefreshActivePage(false); });
        AddNav("switch", DashboardIconRegistry.IconAssetSync, IsGerman ? "YACA wechseln" : "Switch YACA", () => ShowSwitchPage());
        AddNav("updater", DashboardIconRegistry.IconAssetSync, "YACA Updater", () => { ShowSwitchPage(); Dispatcher.BeginInvoke(new Action(() => SetActiveNav("updater")), System.Windows.Threading.DispatcherPriority.Background); });
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("backup-create", DashboardIconRegistry.IconAssetBackup, IsGerman ? "Backup erstellen" : "Create Backup", CreateBackupFromDashboard);
        AddNav("backups", DashboardIconRegistry.IconAssetBackups, IsGerman ? "Backup verwalten" : "Manage Backups", ShowBackups);
        NavPanel.Children.Add(new Separator { Margin = new Thickness(10, 12, 0, 12), Background = (Brush)FindResource("AccentSoftBrush") });
        AddNav("info", DashboardIconRegistry.IconAssetInfo, "Info & Links", ShowInfo);

        ExitNavContent.Children.Clear();
        ConfigureNavContent(ExitNavContent, DashboardIconRegistry.IconAssetExit, IsGerman ? "Beenden" : "Exit");
    }

    private static void ConfigureNavContent(StackPanel content, string iconAssetKey, string text, Brush? iconBrush = null)
    {
        content.Orientation = Orientation.Horizontal;
        content.VerticalAlignment = VerticalAlignment.Center;
        content.Children.Clear();
        content.Children.Add(DashboardIconRegistry.CreateIcon(iconAssetKey, iconBrush ?? Application.Current.FindResource("ForegroundBrush") as Brush ?? Brushes.White, 30, 30));
        content.Children.Add(new TextBlock { Text = text, FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) });
    }

    private void AddNav(string key, string iconAssetKey, string text, Action action)
    {
        var content = new StackPanel();
        ConfigureNavContent(content, iconAssetKey, text);
        var button = new Button { Style = (Style)FindResource("NavButtonStyle"), Height = 46, Tag = key, Content = content };
        button.Click += (_, _) => action(); NavPanel.Children.Add(button); _navButtons.Add((key, button));
    }

    private void SetActiveNav(string key) { _activePage = key; foreach (var item in _navButtons) { var selected = item.Key.Equals(key, StringComparison.OrdinalIgnoreCase); item.Button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent; item.Button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"); item.Button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent; item.Button.BorderThickness = selected ? new Thickness(1) : new Thickness(0); if (item.Button.Content is StackPanel panel && panel.Children.OfType<System.Windows.Controls.Image>().FirstOrDefault() is { } icon) DashboardIconRegistry.SetFill(icon, selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush")); } }
    private void LoadLanguageSelector() { LanguageCombo.Items.Clear(); LanguageCombo.Items.Add(Texts.LanguageGerman); LanguageCombo.Items.Add(Texts.LanguageEnglish); LanguageCombo.SelectedIndex = IsGerman ? 0 : 1; }
    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (!IsInitialized || LanguageCombo.SelectedIndex < 0) return; var language = LanguageCombo.SelectedIndex == 0 ? Localization.German : Localization.English; if (string.Equals(Localization.Normalize(_service.Settings.Language), language, StringComparison.OrdinalIgnoreCase)) return; _service.Settings.Language = language; _service.Settings.Save(); BuildNavigation(); LoadLanguageSelector(); ShowCurrentPageAfterLanguageChange(); }
    private void ShowCurrentPageAfterLanguageChange() { switch (_activePage) { case "switch": ShowSwitchPage(); break; case "backups": ShowBackups(); break; case "config": ShowConfig(); break; case "info": ShowInfo(); break; default: ShowHome(); break; } }
    private void SetGlobalStatus(string message, bool success = false) { GlobalFooterStatusText.Text = message; GlobalFooterStatusText.Foreground = (Brush)FindResource(success ? "SuccessBrush" : "ForegroundBrush"); GlobalFooterStatusText.FontWeight = success ? FontWeights.Bold : FontWeights.Normal; }

    private void ShowHome() { _activePage = "home"; PageHost.Content = RenderDashboard(); SetActiveNav("home"); SetGlobalStatus(IsGerman ? "Bereit." : "Ready."); RefreshHome(); }

    private void RefreshHome(bool announce = false)
    {
        if (_activePage != "home") return; try { _plugins.Clear(); _plugins.AddRange(GetDistinctPlugins()); var current = _service.DetectCurrent(); UpdateCurrentInstalled(current); var running = TeamSpeakDetector.IsRunning(); _tsStatus!.Text = running ? (IsGerman ? "GESTARTET" : "RUNNING") : (IsGerman ? "NICHT GESTARTET" : "NOT RUNNING"); _tsStatus.Foreground = running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("SuccessBrush"); _tsDescription!.Text = running ? (IsGerman ? "TeamSpeak 3 ist aktiv!\nFür einen sicheren Wechsel bitte zuerst schliessen." : "TeamSpeak 3 is active!\nFor a safe switch, please close it first.") : (IsGerman ? "TeamSpeak 3 ist nicht aktiv.\nWechsel jederzeit möglich." : "TeamSpeak 3 is not active.\nSwitching is ready."); _tsClose!.Visibility = running ? Visibility.Visible : Visibility.Collapsed; UpdateBackupSummary(_service.Backups.ListBackups().FirstOrDefault()); RenderVersionList(current); if (announce) SetGlobalStatus(running ? Texts.TeamspeakRunning : Texts.TeamspeakStopped); } catch (Exception ex) { _service.Logger.Error($"Dashboard refresh failed: {ex}"); ShowError(Texts.ErrorUnexpected); }
    }

    private void UpdateCurrentInstalled(YacaPluginInfo? current) { _currentValue!.Text = current?.Version?.ToString() ?? (File.Exists(_service.TargetFile) ? Texts.UnknownInvalid : Texts.NotInstalled); _currentValue.Foreground = current is null ? (Brush)FindResource("WarningBrush") : (Brush)FindResource("ForegroundBrush"); if (_currentDetails is not null) _currentDetails.Text = current is null ? string.Empty : $"Build: YACA {current.Version} - {current.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"}\nGröße: {current.FileSize.ToString("N0", CultureInfo.GetCultureInfo("de-DE"))} Bytes\nSHA-256\n────────────────────\n{current.Sha256}"; }
    private List<YacaPluginInfo> GetDistinctPlugins() { var result = new List<YacaPluginInfo>(); var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase); foreach (var p in _service.ScanPlugins()) if (seen.Add($"{p.FilePath}|{p.Sha256}")) result.Add(p); return result; }

    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch"; SetActiveNav("switch"); var root = new Grid { Margin = new Thickness(0, 4, 0, 0) }; root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var left = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("AccentBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(6) }; var leftPanel = new Grid(); leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var leftHeader = new Grid(); leftHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); leftHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetSync, IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS", (Brush)FindResource("AccentBrush")); Grid.SetColumn(header, 0); leftHeader.Children.Add(header);
        var list = new StackPanel { Margin = new Thickness(6, 10, 6, 6) };
        var sortButton = new Button { Width = 34, Height = 34, Margin = new Thickness(8, 0, 0, 0), Background = Brushes.Transparent, BorderBrush = (Brush)FindResource("AccentBrush"), Foreground = (Brush)FindResource("AccentBrush"), ToolTip = IsGerman ? "Sortierung umschalten" : "Toggle sort order", Content = DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetSort, (Brush)FindResource("AccentBrush"), 20, 20) }; sortButton.Click += (_, _) => { _switchSortDescending = !_switchSortDescending; RenderSwitchVersionList(list, currentForSort: _service.DetectCurrent()); }; Grid.SetColumn(sortButton, 1); leftHeader.Children.Add(sortButton); Grid.SetRow(leftHeader, 0); leftPanel.Children.Add(leftHeader);
        var scroll = new ScrollViewer { Content = list, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Background = (Brush)FindResource("SurfaceBrush") }; Grid.SetRow(scroll, 1); leftPanel.Children.Add(scroll); left.Child = leftPanel; Grid.SetColumn(left, 0); root.Children.Add(left);
        var right = new Grid { Margin = new Thickness(6) }; right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        var updaterCard = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("GoldBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(0, 0, 0, 6) }; var updaterPanel = new Grid(); updaterPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); updaterPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); updaterPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); updaterPanel.Children.Add(CreateDashboardHeader(DashboardIconRegistry.IconAssetSync, "DOWNLOADER", (Brush)FindResource("GoldBrush")));
        var updateContent = new StackPanel { Margin = new Thickness(6, 14, 6, 6), VerticalAlignment = VerticalAlignment.Center }; _updaterVersion = new TextBlock { Text = IsGerman ? "Bereit für Updates" : "Ready for updates", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("ForegroundBrush"), HorizontalAlignment = HorizontalAlignment.Center }; _updaterStatus = new TextBlock { Text = IsGerman ? "Neue YACA Versionen können hier heruntergeladen werden." : "New YACA versions can be downloaded here.", FontSize = 14, Foreground = (Brush)FindResource("SecondaryBrush"), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 12), TextWrapping = TextWrapping.Wrap }; _updaterProgress = new ProgressBar { Height = 10, Minimum = 0, Maximum = 100, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 0, 0, 10) }; _updaterSize = new TextBlock { FontSize = 12, Foreground = (Brush)FindResource("SecondaryBrush"), HorizontalAlignment = HorizontalAlignment.Center }; updateContent.Children.Add(_updaterVersion); updateContent.Children.Add(_updaterStatus); updateContent.Children.Add(_updaterProgress); updateContent.Children.Add(_updaterSize); Grid.SetRow(updateContent, 1); updaterPanel.Children.Add(updateContent);
        var updateButton = new Button { Content = IsGerman ? "YACA UPDATES PRÜFEN UND HERUNTERLADEN" : "CHECK FOR YACA UPDATES AND DOWNLOAD", Height = 42, FontWeight = FontWeights.Bold, Background = (Brush)FindResource("GoldBrush"), Foreground = Brushes.Black, BorderThickness = new Thickness(0), Margin = new Thickness(6, 4, 6, 0), Cursor = Cursors.Hand }; updateButton.Click += async (_, _) => await RunUpdaterAsync(); Grid.SetRow(updateButton, 2); updaterPanel.Children.Add(updateButton); updaterCard.Child = updaterPanel; Grid.SetRow(updaterCard, 0); right.Children.Add(updaterCard);
        var filesCard = new Border { Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("GoldBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Margin = new Thickness(0, 6, 0, 0) }; var filesPanel = new Grid(); filesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); filesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); filesPanel.Children.Add(CreateDashboardHeader(DashboardIconRegistry.IconAssetBackup, IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES", (Brush)FindResource("GoldBrush"))); _downloadedFilesPanel = new StackPanel { Margin = new Thickness(6, 10, 6, 6) }; var filesScroll = new ScrollViewer { Content = _downloadedFilesPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Background = (Brush)FindResource("SurfaceBrush") }; Grid.SetRow(filesScroll, 1); filesPanel.Children.Add(filesScroll); filesCard.Child = filesPanel; Grid.SetRow(filesCard, 1); right.Children.Add(filesCard); Grid.SetColumn(right, 1); root.Children.Add(right);
        var current = _service.DetectCurrent(); RenderSwitchVersionList(list, current); PageHost.Content = root; SetGlobalStatus(status ?? (IsGerman ? "Bereit." : "Ready.")); _ = RefreshDownloadedFilesAsync();
    }

    private void RenderSwitchVersionList(StackPanel list, YacaPluginInfo? currentForSort)
    {
        list.Children.Clear(); var ordered = _switchSortDescending ? GetDistinctPlugins().OrderByDescending(p => p.Version).ThenByDescending(p => p.Build).ToList() : GetDistinctPlugins().OrderBy(p => p.Version).ThenBy(p => p.Build).ToList(); foreach (var p in ordered) { var active = currentForSort?.Sha256.Equals(p.Sha256, StringComparison.OrdinalIgnoreCase) == true; var b = new Button { Style = (Style)FindResource("TileButtonStyle"), BorderBrush = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("AccentBrush"), Margin = new Thickness(0, 2, 0, 2), Height = 58, HorizontalContentAlignment = HorizontalAlignment.Left, Content = new TextBlock { Text = active ? $"YACA {p.Version} - (Build: {p.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"})   —   {Texts.Active.TrimEnd(':')}" : $"YACA {p.Version} - (Build: {p.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"})", FontSize = 15, Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush") } }; b.Click += (_, _) => Activate(p); list.Children.Add(b); }
    }

    private async Task RunUpdaterAsync()
    {
        if (_updaterCts is not null) return; _updaterCts = new CancellationTokenSource(); _updaterProgress!.Visibility = Visibility.Visible; _updaterProgress.Value = 0; _updaterStatus!.Text = IsGerman ? "Suche nach verfügbaren YACA Versionen …" : "Checking for available YACA versions …"; SetGlobalStatus(_updaterStatus.Text);
        var progress = new Progress<YacaUpdaterProgress>(p => { _updaterVersion!.Text = $"YACA {p.Version}"; _updaterStatus!.Text = p.Status; if (p.TotalBytes is > 0) _updaterProgress!.Value = Math.Min(100, p.BytesReceived * 100d / p.TotalBytes.Value); _updaterSize!.Text = p.TotalBytes is > 0 ? $"{p.BytesReceived / 1024d / 1024d:0.00} MB / {p.TotalBytes.Value / 1024d / 1024d:0.00} MB" : string.Empty; if (p.Completed && p.Success) SetGlobalStatus($"YACA {p.Version}: {p.Status}", true); else if (p.Completed && !p.Success) SetGlobalStatus($"YACA {p.Version}: {p.Status}"); });
        try { var before = (await _updater.GetMissingVersionsAsync(_updaterCts.Token)).Count; await _updater.DownloadMissingAsync(progress, _updaterCts.Token); await RefreshDownloadedFilesAsync(); _plugins.Clear(); _plugins.AddRange(GetDistinctPlugins()); var after = (await _updater.GetMissingVersionsAsync(_updaterCts.Token)).Count; ShowSwitchPage(); SetGlobalStatus(before == 0 || after >= before ? (IsGerman ? "Keine neuen YACA Downloads verfügbar" : "No new YACA downloads available") : (IsGerman ? "YACA Downloads aktualisiert." : "YACA downloads refreshed."), after < before); }
        catch (OperationCanceledException) { SetGlobalStatus(IsGerman ? "YACA Update abgebrochen." : "YACA update cancelled."); }
        catch (Exception ex) { _service.Logger.Error($"YACA updater failed: {ex}"); SetGlobalStatus(IsGerman ? "YACA Update fehlgeschlagen." : "YACA update failed."); }
        finally { _updaterCts.Dispose(); _updaterCts = null; }
    }

    private async Task RefreshDownloadedFilesAsync() { if (_downloadedFilesPanel is null) return; var files = await _updater.GetAvailableDownloadsAsync(); _downloadedFilesPanel.Children.Clear(); if (files.Count == 0) { _downloadedFilesPanel.Children.Add(new TextBlock { Text = IsGerman ? "Noch keine Downloads vorhanden." : "No downloads yet.", Foreground = (Brush)FindResource("SecondaryBrush"), FontSize = 14 }); return; } foreach (var file in files) { var row = new Grid { MinHeight = 38, Margin = new Thickness(0, 2, 0, 2) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.Children.Add(new TextBlock { Text = $"YACA {file.Version}", FontSize = 15, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center }); var size = new TextBlock { Text = $"{file.Size / 1024d / 1024d:0.00} MB", FontSize = 13, Foreground = (Brush)FindResource("SecondaryBrush"), VerticalAlignment = VerticalAlignment.Center }; Grid.SetColumn(size, 1); row.Children.Add(size); _downloadedFilesPanel.Children.Add(row); } }

    private void Activate(YacaPluginInfo plugin) { var text = Texts; var current = _service.DetectCurrent(); if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true) { SetGlobalStatus(text.AlreadyActiveMessage); return; } if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning()) { SetGlobalStatus(text.TeamspeakRunningMessage); return; } try { Mouse.OverrideCursor = Cursors.Wait; _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups); ShowSwitchPage($"{plugin.DisplayName} {text.ActivatedMessage}"); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException) { _service.Logger.Error($"YACA switch failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); } finally { Mouse.OverrideCursor = null; } }
    private void CreateBackupFromDashboard() { var text = Texts; if (TeamSpeakDetector.IsRunning() && _service.Settings.WarnIfTeamSpeakRunning) { SetGlobalStatus(text.TeamspeakRunningMessage); GlobalFooterStatusText.Foreground = (Brush)FindResource("ErrorBrush"); GlobalFooterStatusText.FontWeight = FontWeights.Bold; return; } try { var current = _service.DetectCurrent(); if (current is null) { ShowError(text.NotInstalled); return; } if (_service.Backups.CreateBackup(_service.TargetFile, current, automatic: false) is null) { ShowError(text.ErrorUnexpected); return; } _service.Backups.Trim(_service.Settings.MaxBackups); RefreshHome(); SetGlobalStatus(IsGerman ? "Backup wurde erfolgreich erstellt." : "Backup created successfully.", true); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException) { _service.Logger.Error($"Dashboard backup failed: {ex}"); ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected)); } }
    private void CloseTeamSpeak() { var text = Texts; if (!TeamSpeakDetector.IsRunning()) { RefreshHome(); return; } if (MessageBox.Show(text.CloseTeamspeakQuestion, text.TeamspeakRunningTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return; try { Mouse.OverrideCursor = Cursors.Wait; if (!TeamSpeakDetector.TryCloseWithElevation(TimeSpan.FromSeconds(10))) ShowError(text.CloseTeamspeakFailed); } catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { ShowError(Localization.GetErrorMessage(ex, text, text.CloseTeamspeakFailed)); } finally { Mouse.OverrideCursor = null; RefreshHome(); } }
    private void ShowBackups() { _activePage = "backups"; SetActiveNav("backups"); PageHost.Content = new BackupView(_service, this); SetGlobalStatus(IsGerman ? "Backupverwaltung geöffnet." : "Backup management opened."); }
    private void ShowConfig() { _activePage = "config"; SetActiveNav("config"); PageHost.Content = new ConfigView(_service, this); SetGlobalStatus(IsGerman ? "Konfiguration geöffnet." : "Configuration opened."); }
    private void ShowInfo() { _activePage = "info"; SetActiveNav("info"); PageHost.Content = new InfoView(_service.Settings.Language); SetGlobalStatus(IsGerman ? "Info & Links geöffnet." : "Info & Links opened."); }
    internal void ReturnHome() => ShowHome();
    private void RefreshActivePage(bool announce) { if (_activePage == "home") RefreshHome(announce); else if (_activePage == "switch") ShowSwitchPage(); else if (_activePage == "backups") PageHost.Content = new BackupView(_service, this); else if (_activePage == "config") PageHost.Content = new ConfigView(_service, this); }
    private void ShowError(string message) => SetGlobalStatus(message);
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ClickCount == 2) { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; return; } if (e.ChangedButton == MouseButton.Left) DragMove(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) { _updaterCts?.Cancel(); Close(); }
}