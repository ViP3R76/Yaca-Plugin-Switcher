using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private Task? _storedDownloadsInitializationTask;
    private Button? _downloadManagementButton;

    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch";
        SetActiveNav("switch");

        var protectedInstalledVersion = false;
        try
        {
            protectedInstalledVersion = _service.EnsureCurrentPluginAvailable();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _service.Logger.Error($"Installed YACA protection failed: {ex}");
            SetGlobalStatus(IsGerman
                ? "Installierte YACA Plugin Version konnte nicht in Plugins bereitgestellt werden."
                : "Installed YACA plugin version could not be made available in Plugins.");
        }

        var root = CreateSwitchPageRoot();
        var current = _service.DetectCurrent();
        var installedList = CreateInstalledVersionsPanel(root);
        CreateAvailableDownloadsPanel(root);
        CreateUpdaterPanel(root);
        CreateDownloadedFilesPanel(root);
        PageHost.Content = root;
        RenderSwitchVersionList(installedList, current);

        if (status is not null)
            SetGlobalStatus(status);

        if (protectedInstalledVersion)
        {
            SetGlobalWarningStatus(IsGerman
                ? "Installierte Yaca Plugin Version in Plugins zur Verfügung gestellt"
                : "Installed Yaca plugin version made available in Plugins");
        }

        _ = InitializeStoredDownloadsAsync(installedList);
    }

    private static Grid CreateSwitchPageRoot()
    {
        var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        return root;
    }

    private StackPanel CreateInstalledVersionsPanel(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(gold, new Thickness(6, 6, 6, 3));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var headerHost = new Grid();
        headerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetInstalled,
            IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", gold);
        Grid.SetColumn(header, 0);
        Grid.SetColumnSpan(header, 2);
        headerHost.Children.Add(header);

        var list = new StackPanel { Margin = new Thickness(6, 10, 6, 6) };
        var sortButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderBrush = gold,
            Foreground = gold,
            ToolTip = IsGerman ? "Sortierung umschalten" : "Toggle sort order",
            Content = DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetSort, gold, 20, 20)
        };
        sortButton.Click += (_, _) =>
        {
            _switchSortDescending = !_switchSortDescending;
            RenderSwitchVersionList(list, _service.DetectCurrent());
        };
        Grid.SetColumn(sortButton, 1);
        headerHost.Children.Add(sortButton);
        Grid.SetRow(headerHost, 0);
        panel.Children.Add(headerHost);

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)FindResource("SurfaceBrush")
        };
        Grid.SetRow(scroll, 1);
        panel.Children.Add(scroll);

        card.Child = panel;
        Grid.SetColumn(card, 0);
        Grid.SetRow(card, 0);
        root.Children.Add(card);
        return list;
    }

    private void CreateAvailableDownloadsPanel(Grid root)
    {
        var purple = (Brush)FindResource("AccentBrush");
        var card = CreatePanelCardForSwitch(purple, new Thickness(6, 3, 6, 6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetSync,
            IsGerman ? "VERFÜGBARE DOWNLOADS" : "AVAILABLE DOWNLOADS", purple);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        _updaterSelectionList = new StackPanel { Margin = new Thickness(6, 2, 6, 2) };
        var versionScroll = new ScrollViewer
        {
            Content = _updaterSelectionList,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)FindResource("ControlBrush")
        };
        _updaterSelectAll = new CheckBox
        {
            Content = IsGerman ? "Alle Versionen auswählen" : "Select all versions",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("GoldBrush"),
            Margin = new Thickness(4, 4, 4, 5)
        };
        _updaterSelectAll.Click += UpdaterSelectAll_Click;
        var cancelButton = new Button
        {
            Content = IsGerman ? "ABBRECHEN" : "CANCEL",
            Height = 36,
            Style = (Style)FindResource("NormalActionButtonStyle"),
            Margin = new Thickness(4, 6, 0, 0)
        };
        cancelButton.Click += CancelUpdaterSelection_Click;
        _updaterSelectionPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };
        _updaterSelectionPanel.Children.Add(_updaterSelectAll);
        _updaterSelectionPanel.Children.Add(versionScroll);
        _updaterSelectionPanel.Children.Add(cancelButton);
        Grid.SetRow(_updaterSelectionPanel, 1);
        panel.Children.Add(_updaterSelectionPanel);

        var hint = new TextBlock
        {
            Text = IsGerman ? "Nach der Suche erscheinen hier die verfügbaren Versionen." : "Available versions appear here after the search.",
            FontSize = 14,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            IsHitTestVisible = false
        };
        Grid.SetRow(hint, 1);
        panel.Children.Add(hint);
        _updaterSelectionPanel.IsVisibleChanged += (_, _) => hint.Visibility =
            _updaterSelectionPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

        card.Child = panel;
        Grid.SetColumn(card, 0);
        Grid.SetRow(card, 1);
        root.Children.Add(card);
    }

    private void CreateUpdaterPanel(Grid root)
    {
        _updaterSearchButton = null;
        _rendererUpdaterStepPanel = null;
        _rendererUpdaterStatusSource = null;
        _rendererUpdaterSteps.Clear();

        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(gold, new Thickness(6, 6, 6, 3));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetSync, "DOWNLOADER", gold);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);
        var content = CreateUpdaterContent();
        Grid.SetRow(content, 1);
        panel.Children.Add(content);

        _updaterSearchButton = new Button
        {
            Content = IsGerman ? "NACH UPDATES SUCHEN" : "CHECK FOR UPDATES",
            Height = 42,
            Style = (Style)FindResource("UpdateSearchButtonStyle"),
            Margin = new Thickness(6, 4, 6, 0),
            Cursor = Cursors.Hand
        };
        _updaterSearchButton.Click += async (_, _) => await RunUpdaterActionAsync();
        Grid.SetRow(_updaterSearchButton, 2);
        panel.Children.Add(_updaterSearchButton);

        card.Child = panel;
        Grid.SetColumn(card, 1);
        Grid.SetRow(card, 0);
        root.Children.Add(card);
    }

    private void CreateDownloadedFilesPanel(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(gold, new Thickness(6, 3, 6, 6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetBackup,
            IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES", gold);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);
        _downloadedFilesPanel = new StackPanel { Margin = new Thickness(6, 10, 6, 6) };
        var filesScroll = new ScrollViewer
        {
            Content = _downloadedFilesPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = (Brush)FindResource("SurfaceBrush")
        };
        Grid.SetRow(filesScroll, 1);
        panel.Children.Add(filesScroll);

        _downloadManagementButton = new Button
        {
            Content = IsGerman ? "DOWNLOADS VERWALTEN" : "MANAGE DOWNLOADS",
            Height = 40,
            Style = (Style)FindResource("NormalActionButtonStyle"),
            Margin = new Thickness(6, 4, 6, 0),
            Visibility = Visibility.Collapsed,
            Cursor = Cursors.Hand
        };
        _downloadManagementButton.Click += (_, _) => ShowBackups();
        Grid.SetRow(_downloadManagementButton, 2);
        panel.Children.Add(_downloadManagementButton);

        card.Child = panel;
        Grid.SetColumn(card, 1);
        Grid.SetRow(card, 1);
        root.Children.Add(card);
    }

    private static Border CreatePanelCardForSwitch(Brush borderBrush, Thickness margin)
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(18, 19, 24)),
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(20),
            Margin = margin
        };
    }

    private StackPanel CreateUpdaterContent()
    {
        var content = new StackPanel
        {
            Margin = new Thickness(6, 14, 6, 6),
            VerticalAlignment = VerticalAlignment.Center
        };
        _updaterVersion = new TextBlock
        {
            Text = IsGerman ? "Bereit für Updates" : "Ready for updates",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _updaterStatus = new TextBlock
        {
            Text = IsGerman ? "Neue YACA Versionen können hier gesucht werden." : "New YACA versions can be searched here.",
            FontSize = 14,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        _updaterProgress = new ProgressBar
        {
            Height = 10,
            Minimum = 0,
            Maximum = 100,
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 0, 0, 10)
        };
        _updaterSize = new TextBlock
        {
            FontSize = 12,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        content.Children.Add(_updaterVersion);
        content.Children.Add(_updaterStatus);
        content.Children.Add(_updaterProgress);
        content.Children.Add(_updaterSize);
        return content;
    }

    private Task EnsureStoredDownloadsProcessedAsync() =>
        _storedDownloadsInitializationTask ??= _updater.ProcessStoredDownloadsAsync();

    private async Task InitializeStoredDownloadsAsync(StackPanel installedList)
    {
        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            await RefreshDownloadedFilesAsync();
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());
            RenderSwitchVersionList(installedList, _service.DetectCurrent());
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Stored YACA plugin processing failed: {ex}");
            SetGlobalStatus(IsGerman ? "Gespeicherte YACA Downloads konnten nicht vollständig geprüft werden." : "Stored YACA downloads could not be fully processed.");
        }
    }

    private void RenderSwitchVersionList(StackPanel list, YacaPluginInfo? currentForSort)
    {
        list.Children.Clear();
        var plugins = GetDistinctPlugins();
        var ordered = _switchSortDescending
            ? plugins.OrderByDescending(plugin => plugin.Version).ThenByDescending(plugin => plugin.Build).ToList()
            : plugins.OrderBy(plugin => plugin.Version).ThenBy(plugin => plugin.Build).ToList();

        foreach (var plugin in ordered)
        {
            var active = currentForSort?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true;
            var button = new Button
            {
                Style = (Style)FindResource("TileButtonStyle"),
                BorderBrush = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("AccentBrush"),
                Margin = new Thickness(0, 2, 0, 2),
                Height = 58,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = CreateVersionButtonContent(plugin, active)
            };
            button.Click += (_, _) => Activate(plugin);
            list.Children.Add(button);
        }
    }

    private TextBlock CreateVersionButtonContent(YacaPluginInfo plugin, bool active)
    {
        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";
        return new TextBlock
        {
            Text = active ? $"YACA {plugin.Version} - (Build: {build})   —   {Texts.Active.TrimEnd(':')}" : $"YACA {plugin.Version} - (Build: {build})",
            FontSize = 15,
            Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush")
        };
    }

    private void Activate(YacaPluginInfo plugin)
    {
        var text = Texts;
        var current = _service.DetectCurrent();
        if (current?.Sha256.Equals(plugin.Sha256, StringComparison.OrdinalIgnoreCase) == true)
        {
            SetGlobalStatus(text.AlreadyActiveMessage);
            return;
        }
        if (_service.Settings.WarnIfTeamSpeakRunning && TeamSpeakDetector.IsRunning())
        {
            SetGlobalStatus(text.TeamspeakRunningMessage);
            return;
        }
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _service.Installer.Install(plugin, _service.TargetFile, current, _service.Settings.AutomaticBackup, _service.Settings.MaxBackups);
            ShowSwitchPage();
            SetPluginSwitchFooterStatus(plugin);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException)
        {
            _service.Logger.Error($"YACA switch failed: {ex}");
            ShowError(Localization.GetErrorMessage(ex, text, text.ErrorUnexpected));
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}
