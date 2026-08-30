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

    /// <summary>
    /// Erstellt die Ansicht zum Wechseln zwischen den verfügbaren YACA-Versionen.
    /// Beim ersten Aufruf werden bereits gespeicherte Plugin-Archive einmalig
    /// durch den zentralen Updater geprüft und bei Bedarf integriert.
    /// </summary>
    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch";
        SetActiveNav("switch");

        var root = CreateSwitchPageRoot();

        var leftPanel = CreateAvailableVersionsPanel(root);
        var versionList = (StackPanel)leftPanel.Tag!;
        _ = versionList;

        CreateUpdaterPanel(root);
        CreateDownloadedFilesPanel(root);

        var current = _service.DetectCurrent();
        RenderSwitchVersionList(versionList, current);
        PageHost.Content = root;

        if (status is not null)
        {
            SetGlobalStatus(status);
        }

        _ = RefreshDownloadedFilesAsync();
        _ = InitializeStoredDownloadsAsync(versionList);
    }

    /// <summary>
    /// Erstellt das Grundlayout der Wechselansicht mit zwei gleich breiten Spalten.
    /// </summary>
    private static Grid CreateSwitchPageRoot()
    {
        var root = new Grid
        {
            Margin = new Thickness(0, 4, 0, 0)
        };

        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        return root;
    }

    /// <summary>
    /// Erstellt das linke Panel mit der Liste der verfügbaren YACA-Versionen.
    /// Das StackPanel wird am Panel als Tag weitergegeben, damit die bestehende
    /// Sortier- und Aktualisierungslogik dieselbe Instanz verwenden kann.
    /// </summary>
    private Border CreateAvailableVersionsPanel(Grid root)
    {
        var accent = (Brush)FindResource("AccentBrush");

        var left = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = accent,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(6)
        };

        var leftPanel = new Grid();
        leftPanel.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = CreateSwitchHeader(accent);
        Grid.SetRow(header, 0);
        leftPanel.Children.Add(header);

        var list = new StackPanel
        {
            Margin = new Thickness(6, 10, 6, 6)
        };

        var sortButton = CreateSortButton(list, accent);
        header.Children.Add(sortButton);

        var scroll = new ScrollViewer
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = (Brush)FindResource("SurfaceBrush")
        };

        Grid.SetRow(scroll, 1);
        leftPanel.Children.Add(scroll);

        left.Child = leftPanel;
        left.Tag = list;
        Grid.SetColumn(left, 0);
        root.Children.Add(left);

        return left;
    }

    /// <summary>
    /// Erstellt den Header des linken Versionspanels inklusive Sortierbutton.
    /// </summary>
    private Grid CreateSwitchHeader(Brush accent)
    {
        var headerHost = new Grid();
        headerHost.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerHost.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        var header = CreateDashboardHeader(
            DashboardIconRegistry.IconAssetSync,
            IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS",
            accent);

        // Der Header spannt beide Spalten. Dadurch bleibt die Trennlinie unterhalb
        // des Sortiericons vollständig durchgehend.
        Grid.SetColumn(header, 0);
        Grid.SetColumnSpan(header, 2);
        headerHost.Children.Add(header);

        return headerHost;
    }

    /// <summary>
    /// Erstellt den Sortierbutton für die Versionsliste.
    /// </summary>
    private Button CreateSortButton(StackPanel list, Brush accent)
    {
        var sortButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Transparent,
            BorderBrush = accent,
            Foreground = accent,
            ToolTip = IsGerman ? "Sortierung umschalten" : "Toggle sort order",
            Content = DashboardIconRegistry.CreateIcon(
                DashboardIconRegistry.IconAssetSort,
                accent,
                20,
                20)
        };

        sortButton.Click += (_, _) =>
        {
            _switchSortDescending = !_switchSortDescending;
            RenderSwitchVersionList(list, _service.DetectCurrent());
        };

        Grid.SetColumn(sortButton, 1);
        return sortButton;
    }

    /// <summary>
    /// Erstellt das rechte Updater-Panel.
    /// </summary>
    private void CreateUpdaterPanel(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var updaterCard = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = gold,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var updaterPanel = new Grid();
        updaterPanel.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        updaterPanel.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        updaterPanel.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });

        var header = CreateDashboardHeader(
            DashboardIconRegistry.IconAssetSync,
            "DOWNLOADER",
            gold);
        Grid.SetRow(header, 0);
        updaterPanel.Children.Add(header);

        var content = CreateUpdaterContent();
        Grid.SetRow(content, 1);
        updaterPanel.Children.Add(content);

        var updateButton = new Button
        {
            Content = IsGerman ? "NACH UPDATES SUCHEN" : "CHECK FOR UPDATES",
            Height = 42,
            Style = (Style)FindResource("UpdateSearchButtonStyle"),
            Margin = new Thickness(6, 4, 6, 0),
            Cursor = Cursors.Hand
        };

        updateButton.Click += async (_, _) => await RunUpdaterAsync();
        Grid.SetRow(updateButton, 2);
        updaterPanel.Children.Add(updateButton);

        updaterCard.Child = updaterPanel;
        Grid.SetRow(updaterCard, 0);
        Grid.SetColumn(updaterCard, 1);
        root.Children.Add(updaterCard);
    }

    /// <summary>
    /// Erstellt die Status- und Fortschrittsanzeigen des Updater-Panels.
    /// </summary>
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
            Text = IsGerman
                ? "Neue YACA Versionen können hier heruntergeladen werden."
                : "New YACA versions can be downloaded here.",
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

    /// <summary>
    /// Erstellt das Panel für bereits heruntergeladene Plugin-Dateien.
    /// </summary>
    private void CreateDownloadedFilesPanel(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var filesCard = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = gold,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(0, 6, 0, 0)
        };

        var filesPanel = new Grid();
        filesPanel.RowDefinitions.Add(
            new RowDefinition { Height = GridLength.Auto });
        filesPanel.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = CreateDashboardHeader(
            DashboardIconRegistry.IconAssetBackup,
            IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES",
            gold);
        Grid.SetRow(header, 0);
        filesPanel.Children.Add(header);

        _downloadedFilesPanel = new StackPanel
        {
            Margin = new Thickness(6, 10, 6, 6)
        };

        var filesScroll = new ScrollViewer
        {
            Content = _downloadedFilesPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = (Brush)FindResource("SurfaceBrush")
        };

        Grid.SetRow(filesScroll, 1);
        filesPanel.Children.Add(filesScroll);

        filesCard.Child = filesPanel;
        Grid.SetRow(filesCard, 1);
        Grid.SetColumn(filesCard, 1);
        root.Children.Add(filesCard);
    }

    /// <summary>
    /// Startet die einmalige Verarbeitung bereits gespeicherter Downloads.
    /// Der Task wird gemeinsam vom Verwaltungsbereich und vom eigentlichen
    /// Updater verwendet, damit keine parallelen Integrationsvorgänge entstehen.
    /// </summary>
    private Task EnsureStoredDownloadsProcessedAsync()
    {
        return _storedDownloadsInitializationTask ??= _updater.ProcessStoredDownloadsAsync();
    }

    /// <summary>
    /// Verarbeitet gespeicherte Downloads und aktualisiert anschließend die Ansicht.
    /// </summary>
    private async Task InitializeStoredDownloadsAsync(StackPanel list)
    {
        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            await RefreshDownloadedFilesAsync();

            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());
            RenderSwitchVersionList(list, _service.DetectCurrent());
        }
        catch (OperationCanceledException)
        {
            // Ein Abbruch wird vom aufrufenden Workflow behandelt.
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"Stored YACA plugin processing failed: {ex}");
            SetGlobalStatus(
                IsGerman
                    ? "Gespeicherte YACA Downloads konnten nicht vollständig geprüft werden."
                    : "Stored YACA downloads could not be fully processed.");
        }
    }

    /// <summary>
    /// Rendert die verfügbaren YACA-Versionen in der gewählten Sortierreihenfolge.
    /// </summary>
    private void RenderSwitchVersionList(StackPanel list, YacaPluginInfo? currentForSort)
    {
        list.Children.Clear();

        var plugins = GetDistinctPlugins();
        var ordered = _switchSortDescending
            ? plugins
                .OrderByDescending(plugin => plugin.Version)
                .ThenByDescending(plugin => plugin.Build)
                .ToList()
            : plugins
                .OrderBy(plugin => plugin.Version)
                .ThenBy(plugin => plugin.Build)
                .ToList();

        foreach (var plugin in ordered)
        {
            var active = currentForSort?.Sha256.Equals(
                plugin.Sha256,
                StringComparison.OrdinalIgnoreCase) == true;

            var button = new Button
            {
                Style = (Style)FindResource("TileButtonStyle"),
                BorderBrush = active
                    ? (Brush)FindResource("SuccessBrush")
                    : (Brush)FindResource("AccentBrush"),
                Margin = new Thickness(0, 2, 0, 2),
                Height = 58,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = CreateVersionButtonContent(plugin, active)
            };

            button.Click += (_, _) => Activate(plugin);
            list.Children.Add(button);
        }
    }

    /// <summary>
    /// Erstellt den Textinhalt eines Versionsbuttons.
    /// </summary>
    private TextBlock CreateVersionButtonContent(YacaPluginInfo plugin, bool active)
    {
        var build = plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—";
        var text = active
            ? $"YACA {plugin.Version} - (Build: {build})   —   {Texts.Active.TrimEnd(':')}"
            : $"YACA {plugin.Version} - (Build: {build})";

        return new TextBlock
        {
            Text = text,
            FontSize = 15,
            Foreground = active
                ? (Brush)FindResource("SuccessBrush")
                : (Brush)FindResource("ForegroundBrush")
        };
    }

    /// <summary>
    /// Aktiviert die ausgewählte YACA-Version.
    /// </summary>
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

            _service.Installer.Install(
                plugin,
                _service.TargetFile,
                current,
                _service.Settings.AutomaticBackup,
                _service.Settings.MaxBackups);

            // Die Ansicht darf den erfolgreichen Wechselstatus nicht überschreiben.
            ShowSwitchPage();
            SetPluginSwitchFooterStatus(plugin);
        }
        catch (Exception ex) when (
            ex is IOException
            or UnauthorizedAccessException
            or InvalidDataException
            or InvalidOperationException
            or YacaOperationException)
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
