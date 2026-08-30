using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Erstellt die Ansicht zum Wechseln zwischen den verfügbaren YACA-Versionen.
    /// </summary>
    private void ShowSwitchPage(string? status = null)
    {
        _activePage = "switch";
        SetActiveNav("switch");

        var root = new Grid
        {
            Margin = new Thickness(0, 4, 0, 0)
        };

        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Linkes Panel: verfügbare YACA-Versionen.
        var left = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("AccentBrush"),
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

        var leftHeader = new Grid();
        leftHeader.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        leftHeader.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        var header = CreateDashboardHeader(
            DashboardIconRegistry.IconAssetSync,
            IsGerman ? "VERFÜGBARE VERSIONEN" : "AVAILABLE VERSIONS",
            (Brush)FindResource("AccentBrush"));

        Grid.SetColumn(header, 0);
        leftHeader.Children.Add(header);

        var list = new StackPanel
        {
            Margin = new Thickness(6, 10, 6, 6)
        };

        var sortButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderBrush = (Brush)FindResource("AccentBrush"),
            Foreground = (Brush)FindResource("AccentBrush"),
            ToolTip = IsGerman ? "Sortierung umschalten" : "Toggle sort order",
            Content = DashboardIconRegistry.CreateIcon(
                DashboardIconRegistry.IconAssetSort,
                (Brush)FindResource("AccentBrush"),
                20,
                20)
        };

        sortButton.Click += (_, _) =>
        {
            _switchSortDescending = !_switchSortDescending;
            RenderSwitchVersionList(
                list,
                currentForSort: _service.DetectCurrent());
        };

        Grid.SetColumn(sortButton, 1);
        leftHeader.Children.Add(sortButton);

        Grid.SetRow(leftHeader, 0);
        leftPanel.Children.Add(leftHeader);

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
        Grid.SetColumn(left, 0);
        root.Children.Add(left);

        // Rechtes Panel: Updater und bereits heruntergeladene Dateien.
        var right = new Grid
        {
            Margin = new Thickness(6)
        };

        right.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        right.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var updaterCard = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("GoldBrush"),
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

        updaterPanel.Children.Add(
            CreateDashboardHeader(
                DashboardIconRegistry.IconAssetSync,
                "DOWNLOADER",
                (Brush)FindResource("GoldBrush")));

        var updateContent = new StackPanel
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

        updateContent.Children.Add(_updaterVersion);
        updateContent.Children.Add(_updaterStatus);
        updateContent.Children.Add(_updaterProgress);
        updateContent.Children.Add(_updaterSize);

        Grid.SetRow(updateContent, 1);
        updaterPanel.Children.Add(updateContent);

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
        right.Children.Add(updaterCard);

        var filesCard = new Border
        {
            Background = (Brush)FindResource("SurfaceBrush"),
            BorderBrush = (Brush)FindResource("GoldBrush"),
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

        filesPanel.Children.Add(
            CreateDashboardHeader(
                DashboardIconRegistry.IconAssetBackup,
                IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES",
                (Brush)FindResource("GoldBrush")));

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
        right.Children.Add(filesCard);

        Grid.SetColumn(right, 1);
        root.Children.Add(right);

        var current = _service.DetectCurrent();
        RenderSwitchVersionList(list, current);

        PageHost.Content = root;
        SetGlobalStatus(status ?? (IsGerman ? "Bereit." : "Ready."));
        _ = RefreshDownloadedFilesAsync();
    }

    /// <summary>
    /// Rendert die verfügbaren YACA-Versionen in der gewählten Sortierreihenfolge.
    /// </summary>
    private void RenderSwitchVersionList(
        StackPanel list,
        YacaPluginInfo? currentForSort)
    {
        list.Children.Clear();

        var ordered = _switchSortDescending
            ? GetDistinctPlugins()
                .OrderByDescending(plugin => plugin.Version)
                .ThenByDescending(plugin => plugin.Build)
                .ToList()
            : GetDistinctPlugins()
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
                Content = new TextBlock
                {
                    Text = active
                        ? $"YACA {plugin.Version} - " +
                          $"(Build: {plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"}) " +
                          $"  —   {Texts.Active.TrimEnd(':')}"
                        : $"YACA {plugin.Version} - " +
                          $"(Build: {plugin.Build?.ToString(CultureInfo.InvariantCulture) ?? "—"})",
                    FontSize = 15,
                    Foreground = active
                        ? (Brush)FindResource("SuccessBrush")
                        : (Brush)FindResource("ForegroundBrush")
                }
            };

            button.Click += (_, _) => Activate(plugin);
            list.Children.Add(button);
        }
    }

    /// <summary>
    /// Aktiviert die ausgewählte YACA-Version.
    /// </summary>
    private void Activate(YacaPluginInfo plugin)
    {
        var text = Texts;
        var current = _service.DetectCurrent();

        if (current?.Sha256.Equals(
                plugin.Sha256,
                StringComparison.OrdinalIgnoreCase) == true)
        {
            SetGlobalStatus(text.AlreadyActiveMessage);
            return;
        }

        if (_service.Settings.WarnIfTeamSpeakRunning
            && TeamSpeakDetector.IsRunning())
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
            ShowError(
                Localization.GetErrorMessage(
                    ex,
                    text,
                    text.ErrorUnexpected));
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}
