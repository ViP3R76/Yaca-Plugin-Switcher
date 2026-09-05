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
        CreateCurrentInstalledPanel(root, current);
        CreateAvailableDownloadsPanel(root);
        CreateUpdaterPanel(root);
        CreateDownloadedFilesPanel(root);
        PageHost.Content = root;

        if (status is not null)
            SetGlobalStatus(status);

        if (protectedInstalledVersion)
        {
            SetGlobalWarningStatus(IsGerman
                ? "Installierte Yaca Plugin Version in Plugins zur Verfügung gestellt"
                : "Installed Yaca plugin version made available in Plugins");
        }

        _ = RefreshDownloadedFilesAsync();
        _ = InitializeStoredDownloadsAsync();
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

    private void CreateCurrentInstalledPanel(Grid root, YacaPluginInfo? current)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var card = CreatePanelCardForSwitch(gold, new Thickness(6, 6, 6, 3));
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(DashboardIconRegistry.IconAssetInstalled,
            IsGerman ? "AKTUELL INSTALLIERT" : "CURRENTLY INSTALLED", gold);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        var content = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        content.Children.Add(new TextBlock
        {
            Text = current is null ? "—" : $"YACA {current.Version}",
            FontSize = 36,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        if (current is not null)
        {
            content.Children.Add(new TextBlock
            {
                Text = current.Build?.ToString(CultureInfo.InvariantCulture) is { } build
                    ? $"Build: {build}"
                    : "Build: —",
                FontSize = 14,
                Foreground = (Brush)FindResource("SecondaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            });
            content.Children.Add(new Border
            {
                Background = (Brush)FindResource("SuccessBrush"),
                CornerRadius = new CornerRadius(0),
                Padding = new Thickness(16, 5, 16, 5),
                Margin = new Thickness(0, 12, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = IsGerman ? "AKTIV" : "ACTIVE",
                    Foreground = Brushes.Black,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold
                }
            });
        }
        Grid.SetRow(content, 1);
        panel.Children.Add(content);

        var sha = new TextBlock
        {
            Text = current is null ? string.Empty : $"SHA-256: {current.Sha256}",
            FontSize = 11,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(sha, 2);
        panel.Children.Add(sha);

        card.Child = panel;
        Grid.SetColumn(card, 0);
        Grid.SetRow(card, 0);
        root.Children.Add(card);
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
        hint.Visibility = Visibility.Visible;
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
        var updaterCard = CreatePanelCardForSwitch(gold, new Thickness(6, 6, 6, 3));
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

        updaterCard.Child = panel;
        Grid.SetColumn(updaterCard, 1);
        Grid.SetRow(updaterCard, 0);
        root.Children.Add(updaterCard);
    }

    private void CreateDownloadedFilesPanel(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var filesCard = CreatePanelCardForSwitch(gold, new Thickness(6, 3, 6, 6));
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

        filesCard.Child = panel;
        Grid.SetColumn(filesCard, 1);
        Grid.SetRow(filesCard, 1);
        root.Children.Add(filesCard);
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

    private async Task InitializeStoredDownloadsAsync()
    {
        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            await RefreshDownloadedFilesAsync();
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());
            UpdateAvailableDownloadsHint();
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

    private void UpdateAvailableDownloadsHint()
    {
        if (_updaterSelectionPanel is null)
            return;

        if (_updaterSelectionPanel.Visibility == Visibility.Visible)
            return;
    }

    private void ShowUpdaterSelection(IReadOnlyList<string> versions)
    {
        if (_updaterSelectionPanel is null || _updaterSelectionList is null || _updaterSelectAll is null)
            return;

        _updaterSelectionList.Children.Clear();
        for (var index = 0; index < versions.Count; index++)
        {
            var version = versions[index];
            var row = new Border
            {
                Background = (Brush)FindResource(index % 2 == 0 ? "SurfaceBrush" : "ControlBrush"),
                BorderBrush = (Brush)FindResource("AccentSoftBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 5, 8, 5),
                MinWidth = 300
            };
            var checkBox = new CheckBox
            {
                Content = $"YACA {version}",
                IsChecked = true,
                FontSize = 14,
                Foreground = (Brush)FindResource("ForegroundBrush")
            };
            checkBox.Checked += UpdaterVersionSelectionChanged;
            checkBox.Unchecked += UpdaterVersionSelectionChanged;
            row.Child = checkBox;
            _updaterSelectionList.Children.Add(row);
        }

        _updaterSelectAll.IsChecked = true;
        _updaterSelectionPanel.Visibility = Visibility.Visible;

        if (_updaterVersion is not null)
            _updaterVersion.Text = IsGerman ? $"{versions.Count} Updates gefunden" : $"{versions.Count} updates found";
        if (_updaterStatus is not null)
            _updaterStatus.Text = IsGerman
                ? "Versionen auswählen und anschließend JETZT DOWNLOADEN drücken."
                : "Select versions and then press DOWNLOAD NOW.";
        UpdateUpdaterActionButtonState();
    }

    private async Task DownloadSelectedUpdaterVersionsAsync()
    {
        var selectedVersions = GetSelectedUpdaterVersions();
        if (selectedVersions.Count == 0)
        {
            SetGlobalStatus(IsGerman ? "Bitte mindestens eine YACA Version auswählen." : "Please select at least one YACA version.");
            return;
        }

        await DownloadUpdaterVersionsAsync(selectedVersions);
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
