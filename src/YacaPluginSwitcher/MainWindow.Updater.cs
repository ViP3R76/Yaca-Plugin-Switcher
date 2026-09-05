using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private CheckBox? _updaterSelectAll;
    private Button? _updaterSearchButton;
    private StackPanel? _updaterSelectionList;
    private StackPanel? _updaterSelectionPanel;

    private async Task RunUpdaterActionAsync()
    {
        if (_updaterSelectionPanel?.Visibility == Visibility.Visible)
        {
            await DownloadSelectedUpdaterVersionsAsync();
            return;
        }

        await RunUpdaterAsync();
    }

    private async Task RunUpdaterAsync()
    {
        if (_updaterCts is not null)
            return;

        _updaterCts = new CancellationTokenSource();
        IReadOnlyList<string> missingVersions = [];
        var downloadAllWithoutPrompt = _service.Settings.DownloadAllPluginsWithoutPrompt;

        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Visible;
            _updaterProgress.Value = 0;
        }

        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Suche nach verfügbaren YACA Versionen …"
                : "Checking for available YACA versions …";
            SetGlobalStatus(_updaterStatus.Text);
        }

        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            missingVersions = await _updater.GetMissingVersionsAsync(_updaterCts.Token);

            if (missingVersions.Count == 0)
            {
                SetGlobalStatus(IsGerman
                    ? "Keine neuen YACA Downloads verfügbar"
                    : "No new YACA downloads available");
                ShowUpdaterReadyState();
                return;
            }

            if (!downloadAllWithoutPrompt)
                ShowUpdaterSelection(missingVersions);
        }
        catch (OperationCanceledException)
        {
            SetGlobalStatus(IsGerman ? "YACA Updateprüfung abgebrochen." : "YACA update check cancelled.");
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"YACA updater check failed: {ex}");
            SetGlobalStatus(IsGerman ? "YACA Updateprüfung fehlgeschlagen." : "YACA update check failed.");
        }
        finally
        {
            _updaterCts.Dispose();
            _updaterCts = null;
        }

        if (downloadAllWithoutPrompt)
            await DownloadUpdaterVersionsAsync(missingVersions);
    }

    private void EnsureUpdaterSelectionControls()
    {
        if (_updaterSelectionPanel is not null)
            return;

        if (_updaterStatus?.Parent is not StackPanel parent)
            return;

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
        _updaterSelectionPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Background = (Brush)FindResource("ControlBrush"),
            Margin = new Thickness(0, 8, 0, 0)
        };
        _updaterSelectionPanel.Children.Add(_updaterSelectAll);
        _updaterSelectionPanel.Children.Add(versionScroll);
        parent.Children.Add(_updaterSelectionPanel);
    }

    private void ShowUpdaterSelection(IReadOnlyList<string> versions)
    {
        EnsureUpdaterSelectionControls();

        if (_updaterSelectionPanel is null || _updaterSelectionList is null || _updaterSelectAll is null)
            return;

        _updaterSelectionList.Children.Clear();
        for (var index = 0; index < versions.Count; index++)
        {
            var checkBox = new CheckBox
            {
                Content = $"YACA {versions[index]}",
                IsChecked = true,
                FontSize = 14,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                Background = (Brush)FindResource(index % 2 == 0 ? "SurfaceBrush" : "ControlBrush"),
                BorderBrush = (Brush)FindResource("AccentSoftBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 6, 8, 6),
                MinWidth = 300,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0)
            };
            checkBox.Checked += UpdaterVersionSelectionChanged;
            checkBox.Unchecked += UpdaterVersionSelectionChanged;
            _updaterSelectionList.Children.Add(checkBox);
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

    private async Task DownloadUpdaterVersionsAsync(IReadOnlyList<string> versions)
    {
        if (versions.Count == 0 || _updaterCts is not null)
            return;

        _updaterCts = new CancellationTokenSource();
        if (_updaterSelectionPanel is not null)
            _updaterSelectionPanel.IsEnabled = false;
        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Visible;
            _updaterProgress.Value = 0;
        }
        if (_updaterSearchButton is not null)
            _updaterSearchButton.IsEnabled = false;

        var progress = new Progress<YacaUpdaterProgress>(UpdateUpdaterProgress);
        try
        {
            await _updater.DownloadSelectedAsync(versions, progress, _updaterCts.Token);
            await RefreshDownloadedFilesAsync();
            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());
            HideUpdaterSelection();
            ShowSwitchPage();
            SetGlobalStatus(IsGerman ? "YACA Downloads aktualisiert." : "YACA downloads refreshed.", true);
        }
        catch (OperationCanceledException)
        {
            SetGlobalStatus(IsGerman ? "YACA Download abgebrochen." : "YACA download cancelled.");
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"YACA updater download failed: {ex}");
            SetGlobalStatus(IsGerman ? "YACA Download fehlgeschlagen." : "YACA download failed.");
        }
        finally
        {
            if (_updaterSelectionPanel is not null)
                _updaterSelectionPanel.IsEnabled = true;
            _updaterCts.Dispose();
            _updaterCts = null;
        }
    }

    private List<string> GetSelectedUpdaterVersions()
    {
        if (_updaterSelectionList is null)
            return [];

        return _updaterSelectionList.Children
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Content?.ToString())
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Select(version => version!.Replace("YACA ", "", StringComparison.OrdinalIgnoreCase).Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void UpdaterSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        if (_updaterSelectAll is null || _updaterSelectionList is null)
            return;

        var isSelected = _updaterSelectAll.IsChecked == true;
        foreach (var checkBox in _updaterSelectionList.Children.OfType<CheckBox>())
            checkBox.IsChecked = isSelected;
        UpdateUpdaterActionButtonState();
    }

    private void UpdaterVersionSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (_updaterSelectAll is null || _updaterSelectionList is null)
            return;

        var boxes = _updaterSelectionList.Children.OfType<CheckBox>().ToList();
        _updaterSelectAll.IsChecked = boxes.Count > 0 && boxes.All(box => box.IsChecked == true);
        UpdateUpdaterActionButtonState();
    }

    private void UpdateUpdaterActionButtonState()
    {
        if (_updaterSearchButton is null)
            return;

        var selectionVisible = _updaterSelectionPanel?.Visibility == Visibility.Visible;
        var hasSelection = GetSelectedUpdaterVersions().Count > 0;
        _updaterSearchButton.Content = selectionVisible
            ? (IsGerman ? "JETZT DOWNLOADEN" : "DOWNLOAD NOW")
            : (IsGerman ? "NACH UPDATES SUCHEN" : "CHECK FOR UPDATES");
        _updaterSearchButton.IsEnabled = !selectionVisible || hasSelection;
    }

    private void CancelUpdaterSelection_Click(object? sender, RoutedEventArgs e)
    {
        HideUpdaterSelection();
        ShowUpdaterReadyState();
        SetGlobalStatus(IsGerman ? "YACA Downloadauswahl verworfen." : "YACA download selection cancelled.");
    }

    private void HideUpdaterSelection()
    {
        if (_updaterSelectionPanel is not null)
            _updaterSelectionPanel.Visibility = Visibility.Collapsed;
        if (_updaterSelectionList is not null)
            _updaterSelectionList.Children.Clear();
        if (_updaterSelectAll is not null)
            _updaterSelectAll.IsChecked = false;
        UpdateUpdaterActionButtonState();
    }

    private void ShowUpdaterReadyState()
    {
        HideUpdaterSelection();
        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Collapsed;
            _updaterProgress.Value = 0;
        }
        if (_updaterVersion is not null)
            _updaterVersion.Text = IsGerman ? "Bereit für Updates" : "Ready for updates";
        if (_updaterStatus is not null)
            _updaterStatus.Text = IsGerman
                ? "Neue YACA Versionen können hier gesucht werden."
                : "New YACA versions can be searched here.";
        UpdateUpdaterActionButtonState();
    }

    private void UpdateUpdaterProgress(YacaUpdaterProgress progress)
    {
        if (_updaterVersion is not null)
            _updaterVersion.Text = $"YACA {progress.Version}";
        if (_updaterStatus is not null)
            _updaterStatus.Text = progress.Status;
        if (_updaterProgress is not null && progress.TotalBytes is > 0)
        {
            _updaterProgress.Value = Math.Min(100, progress.BytesReceived * 100d / progress.TotalBytes.Value);
        }
        if (_updaterSize is not null)
        {
            _updaterSize.Text = progress.TotalBytes is > 0
                ? $"{progress.BytesReceived / 1024d / 1024d:0.00} MB / {progress.TotalBytes.Value / 1024d / 1024d:0.00} MB"
                : string.Empty;
        }
        if (!progress.Completed)
            return;
        SetGlobalStatus($"YACA {progress.Version}: {progress.Status}", progress.Success);
    }

    private async Task RefreshDownloadedFilesAsync()
    {
        if (_downloadedFilesPanel is null)
            return;

        var files = await _updater.GetAvailableDownloadsAsync();
        _downloadedFilesPanel.Children.Clear();

        if (files.Count == 0)
        {
            _downloadedFilesPanel.Children.Add(new TextBlock
            {
                Text = IsGerman ? "Noch keine Downloads vorhanden." : "No downloads yet.",
                Foreground = (Brush)FindResource("SecondaryBrush"),
                FontSize = 14
            });
            if (_downloadManagementButton is not null)
                _downloadManagementButton.Visibility = Visibility.Collapsed;
            return;
        }

        if (_downloadManagementButton is not null)
            _downloadManagementButton.Visibility = Visibility.Visible;

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var row = new Grid
            {
                MinHeight = 38,
                Background = (Brush)FindResource(index % 2 == 0 ? "SurfaceBrush" : "ControlBrush"),
                Margin = new Thickness(0, 0, 0, 1)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(new TextBlock
            {
                Text = $"YACA {file.Version}",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            });
            var size = new TextBlock
            {
                Text = $"{file.Size / 1024d / 1024d:0.00} MB",
                FontSize = 13,
                Foreground = (Brush)FindResource("SecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0)
            };
            Grid.SetColumn(size, 1);
            row.Children.Add(size);
            _downloadedFilesPanel.Children.Add(row);
        }
    }
}
