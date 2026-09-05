using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
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
        var automaticDownloads = _service.Settings.DownloadAllPluginsWithoutPrompt;
        var installedList = CreateInstalledVersionsPanel(root, automaticDownloads);
        _installedVersionList = installedList;

        if (!automaticDownloads)
            CreateAvailableDownloadsPanel(root);

        CreateUpdaterPanel(root);
        CreateDownloadedFilesPanel(root, automaticDownloads);
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

        _ = InitializeStoredDownloadsAsync();
    }

    private static Grid CreateSwitchPageRoot()
    {
        var root = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });
        return root;
    }

    private StackPanel CreateInstalledVersionsPanel(Grid root, bool automaticDownloads)
    {
        var purple = (Brush)FindResource("AccentBrush");
        var card = CreatePanelCardForSwitch(purple, new Thickness(6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });

        var headerHost = new Grid();
        headerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerHost.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetInstalled,
            IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", purple);
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
            BorderBrush = purple,
            Foreground = purple,
            ToolTip = IsGerman ? "Sortierung umschalten" : "Toggle sort order",
            Content = DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetSort, purple, 20, 20)
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
        Grid.SetRowSpan(card, automaticDownloads ? 2 : 1);
        root.Children.Add(card);
        return list;
    }

    private void CreateAvailableDownloadsPanel(Grid root)
    {
        var yellow = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(yellow, new Thickness(6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetSync,
            IsGerman ? "VERFÜGBARE DOWNLOADS" : "AVAILABLE DOWNLOADS", yellow);
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
            Foreground = yellow,
            Margin = new Thickness(4, 4, 4, 5),
            VerticalAlignment = VerticalAlignment.Center,
            Style = (Style)FindResource("DarkCheckBoxStyle")
        };
        _updaterSelectAll.Click += UpdaterSelectAll_Click;

        _updaterDownloadButton = new Button
        {
            Content = IsGerman ? "DOWNLOADEN" : "DOWNLOAD",
            Height = 36,
            Style = (Style)FindResource("UpdateSearchButtonStyle"),
            Margin = new Thickness(4, 6, 4, 0),
            Visibility = Visibility.Collapsed,
            Cursor = Cursors.Hand
        };
        _updaterDownloadButton.Click += async (_, _) => await DownloadSelectedUpdaterVersionsAsync();

        _updaterCancelButton = new Button
        {
            Content = IsGerman ? "ABBRECHEN" : "CANCEL",
            Height = 36,
            Style = (Style)FindResource("NormalActionButtonStyle"),
            Margin = new Thickness(4, 6, 4, 0),
            Cursor = Cursors.Hand
        };
        _updaterCancelButton.Click += CancelUpdaterSelection_Click;

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        actions.Children.Add(_updaterDownloadButton);
        actions.Children.Add(_updaterCancelButton);

        _updaterSelectionPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Margin = new Thickness(0, 8, 0, 0)
        };
        _updaterSelectionPanel.Children.Add(_updaterSelectAll);
        _updaterSelectionPanel.Children.Add(versionScroll);
        _updaterSelectionPanel.Children.Add(actions);
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
        Grid.SetColumn(card, 1);
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
        var card = CreatePanelCardForSwitch(gold, new Thickness(6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });
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
        RestoreCachedUpdaterState();
    }

    private void CreateDownloadedFilesPanel(Grid root, bool automaticDownloads)
    {
        var yellow = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(yellow, new Thickness(6));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star), MinHeight = 0 });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetBackup,
            IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES", yellow);
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
            Style = (Style)FindResource("UpdateSearchButtonStyle"),
            Margin = new Thickness(6, 4, 6, 0),
            Visibility = Visibility.Collapsed,
            Cursor = Cursors.Hand
        };
        _downloadManagementButton.Click += (_, _) => ShowBackups();
        Grid.SetRow(_downloadManagementButton, 2);
        panel.Children.Add(_downloadManagementButton);

        card.Child = panel;
        Grid.SetColumn(card, automaticDownloads ? 1 : 0);
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
            Text = IsGerman ? "Bereit auf Updates zu prüfen" : "Ready to check for updates",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _updaterStatus = new TextBlock
        {
            Text = IsGerman ? "Updateprüfung für neuere Yaca Plugin Versionen" : "Check for newer Yaca Plugin versions",
            FontSize = 14,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        _updaterFoundVersionsSummary = new TextBlock
        {
            Visibility = Visibility.Collapsed,
            FontSize = 13,
            Foreground = (Brush)FindResource("SuccessBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
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
        content.Children.Add(_updaterFoundVersionsSummary);
        content.Children.Add(_updaterProgress);
        content.Children.Add(_updaterSize);
        return content;
    }

    private Task EnsureStoredDownloadsProcessedAsync() => _updater.ProcessStoredDownloadsAsync();

    private async Task InitializeStoredDownloadsAsync()
    {
        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            await RefreshDownloadedFilesAsync();
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());

            if (_activePage == "switch" && _installedVersionList is not null)
                RenderSwitchVersionList(_installedVersionList, _service.DetectCurrent());
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
        // Keep the ScrollViewer's direct child stable. WPF can retain stale
        // scroll/layout state when a StackPanel inside a ScrollViewer is
        // repeatedly cleared and repopulated. ItemsControl owns the dynamic
        // item collection and correctly invalidates its layout when Items.Clear/
        // Items.Add is used, so the installed list refreshes reliably.
        var items = list.Children.OfType<ItemsControl>().FirstOrDefault();
        if (items is null)
        {
            items = new ItemsControl
            {
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0)
            };
            list.Children.Clear();
            list.Children.Add(items);
        }
        else
        {
            items.Items.Clear();
        }

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
                Height = 50,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = CreateVersionButtonContent(plugin, active)
            };
            button.Click += (_, _) => Activate(plugin);
            items.Items.Add(button);
        }
    }

    private Grid CreateVersionButtonContent(YacaPluginInfo plugin, bool active)
    {
        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";
        var content = new Grid { VerticalAlignment = VerticalAlignment.Center };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var versionText = new TextBlock
        {
            Text = $"YACA {plugin.Version} - (Build: {build})",
            FontSize = 15,
            Foreground = active ? (Brush)FindResource("SuccessBrush") : (Brush)FindResource("ForegroundBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetColumn(versionText, 0);
        content.Children.Add(versionText);

        if (active)
        {
            var badge = new Border
            {
                Background = (Brush)FindResource("SuccessBrush"),
                BorderBrush = (Brush)FindResource("SuccessBrush"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 3, 9, 3),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            badge.Child = new TextBlock
            {
                Text = IsGerman ? "INSTALLIERT" : "INSTALLED",
                Foreground = Brushes.Black,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetColumn(badge, 1);
            content.Children.Add(badge);
        }

        return content;
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
