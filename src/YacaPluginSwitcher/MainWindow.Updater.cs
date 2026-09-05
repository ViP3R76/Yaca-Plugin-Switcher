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
    private Button? _updaterDownloadButton;
    private Button? _updaterCancelButton;
    private TextBlock? _updaterFoundVersionsSummary;
    private string[] _pendingUpdaterDownloads = [];
    private string[] _cachedUpdaterVersions = [];
    private string[] _cachedUpdaterSelection = [];
    private CancellationTokenSource? _updaterNoUpdatesMessageCts;
    private bool _updaterDownloadInProgress;

    private async Task RunUpdaterActionAsync()
    {
        if (_service.Settings.DownloadAllPluginsWithoutPrompt && _pendingUpdaterDownloads.Length > 0)
        {
            var pending = _pendingUpdaterDownloads;
            _pendingUpdaterDownloads = [];
            await DownloadUpdaterVersionsAsync(pending);
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
        _pendingUpdaterDownloads = [];
        _cachedUpdaterVersions = [];
        _cachedUpdaterSelection = [];

        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Visible;
            _updaterProgress.Value = 0;
        }

        try
        {
            await EnsureStoredDownloadsProcessedAsync();
            missingVersions = await _updater.GetMissingVersionsAsync(_updaterCts.Token);

            if (missingVersions.Count == 0)
            {
                ClearCachedUpdaterResults();
                ShowNoUpdatesMessage();
                SetGlobalStatus(IsGerman
                    ? "Keine neuen YACA Downloads verfügbar"
                    : "No new YACA downloads available");
                return;
            }

            _cachedUpdaterVersions = missingVersions.ToArray();

            if (downloadAllWithoutPrompt)
            {
                _pendingUpdaterDownloads = missingVersions.ToArray();
                ShowBulkDownloadReadyState(missingVersions);
            }
            else
            {
                ShowUpdaterSelection(missingVersions);
            }
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
    }

    private void ShowNoUpdatesMessage()
    {
        _updaterNoUpdatesMessageCts?.Cancel();

        var cts = new CancellationTokenSource();
        _updaterNoUpdatesMessageCts = cts;

        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Collapsed;
            _updaterProgress.Value = 0;
        }

        var successBrush = (Brush)FindResource("SuccessBrush");
        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman
                ? "Keine neuen YACA Versionen verfügbar"
                : "No new YACA versions available";
            _updaterVersion.Foreground = successBrush;
        }

        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Die Updateprüfung ist abgeschlossen."
                : "The update check is complete.";
            _updaterStatus.Foreground = successBrush;
        }

        if (_updaterFoundVersionsSummary is not null)
        {
            _updaterFoundVersionsSummary.Text = string.Empty;
            _updaterFoundVersionsSummary.Visibility = Visibility.Collapsed;
        }

        _ = RestoreUpdaterReadyStateAfterNoUpdatesAsync(cts);
    }

    private async Task RestoreUpdaterReadyStateAfterNoUpdatesAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), cts.Token);
            if (cts.IsCancellationRequested
                || !ReferenceEquals(_updaterNoUpdatesMessageCts, cts)
                || _updaterCts is not null
                || _updaterDownloadInProgress)
            {
                return;
            }

            ShowUpdaterReadyState();
            UpdateUpdaterCopy();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_updaterNoUpdatesMessageCts, cts))
                _updaterNoUpdatesMessageCts = null;
            cts.Dispose();
        }
    }

    private void ShowBulkDownloadReadyState(IReadOnlyList<string> versions)
    {
        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Collapsed;
            _updaterProgress.Value = 0;
        }

        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman ? $"{versions.Count} Downloads verfügbar" : $"{versions.Count} downloads available";
            _updaterVersion.Foreground = (Brush)FindResource("ForegroundBrush");
        }
        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Alle fehlenden oder neuen YACA Plugins sind bereit zum Download."
                : "All missing or new YACA plugins are ready to download.";
            _updaterStatus.Foreground = (Brush)FindResource("SecondaryBrush");
        }
        if (_updaterFoundVersionsSummary is not null)
        {
            _updaterFoundVersionsSummary.Text = IsGerman
                ? $"Gefundene Versionen\n{string.Join(" | ", versions)}"
                : $"Found versions\n{string.Join(" | ", versions)}";
            _updaterFoundVersionsSummary.Foreground = (Brush)FindResource("SuccessBrush");
            _updaterFoundVersionsSummary.Visibility = Visibility.Visible;
        }
        if (_updaterSearchButton is not null)
        {
            _updaterSearchButton.Content = IsGerman ? "DOWNLOAD STARTEN" : "START DOWNLOAD";
            _updaterSearchButton.IsEnabled = true;
        }
    }

    private void ShowUpdaterSelection(IReadOnlyList<string> versions, IReadOnlyCollection<string>? selectedVersions = null)
    {
        if (_updaterSelectionPanel is null || _updaterSelectionList is null || _updaterSelectAll is null)
            return;

        _updaterSelectionList.Children.Clear();
        if (_updaterFoundVersionsSummary is not null)
            _updaterFoundVersionsSummary.Visibility = Visibility.Collapsed;

        for (var index = 0; index < versions.Count; index++)
        {
            var row = new Border
            {
                Background = (Brush)FindResource(index % 2 == 0 ? "SurfaceBrush" : "ControlBrush"),
                BorderBrush = (Brush)FindResource("AccentSoftBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                MinHeight = 38,
                Margin = new Thickness(0)
            };
            var checkBox = new CheckBox
            {
                Content = $"YACA {versions[index]}",
                IsChecked = selectedVersions is null || selectedVersions.Contains(versions[index], StringComparer.OrdinalIgnoreCase),
                FontSize = 14,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                Padding = new Thickness(8, 6, 8, 6),
                MinWidth = 300,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0)
            };
            checkBox.Checked += UpdaterVersionSelectionChanged;
            checkBox.Unchecked += UpdaterVersionSelectionChanged;
            checkBox.MouseEnter += (_, _) =>
            {
                row.Background = (Brush)FindResource("ControlHoverBrush");
                checkBox.Foreground = (Brush)FindResource("GoldBrush");
            };
            checkBox.MouseLeave += (_, _) =>
            {
                row.Background = (Brush)FindResource(index % 2 == 0 ? "SurfaceBrush" : "ControlBrush");
                checkBox.Foreground = (Brush)FindResource("ForegroundBrush");
            };
            row.Child = checkBox;
            _updaterSelectionList.Children.Add(row);
        }

        _updaterSelectAll.IsChecked = _updaterSelectionList.Children
            .OfType<Border>()
            .Select(border => border.Child)
            .OfType<CheckBox>()
            .All(checkBox => checkBox.IsChecked == true);
        _updaterSelectionPanel.Visibility = Visibility.Visible;

        _cachedUpdaterVersions = versions.ToArray();
        _cachedUpdaterSelection = GetSelectedUpdaterVersions().ToArray();

        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman ? $"{versions.Count} Updates gefunden" : $"{versions.Count} updates found";
            _updaterVersion.Foreground = (Brush)FindResource("ForegroundBrush");
        }
        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Versionen auswählen und anschließend DOWNLOADEN drücken."
                : "Select versions and then press DOWNLOAD.";
            _updaterStatus.Foreground = (Brush)FindResource("SecondaryBrush");
        }
        UpdateUpdaterActionButtonState();
    }

    private void RestoreCachedUpdaterState()
    {
        if (_cachedUpdaterVersions.Length == 0)
            return;

        if (_service.Settings.DownloadAllPluginsWithoutPrompt)
        {
            _pendingUpdaterDownloads = _cachedUpdaterVersions.ToArray();
            ShowBulkDownloadReadyState(_cachedUpdaterVersions);
            return;
        }

        ShowUpdaterSelection(
            _cachedUpdaterVersions,
            _cachedUpdaterSelection.Length == 0 ? _cachedUpdaterVersions : _cachedUpdaterSelection);
    }

    private void ClearCachedUpdaterResults()
    {
        _pendingUpdaterDownloads = [];
        _cachedUpdaterVersions = [];
        _cachedUpdaterSelection = [];
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
        _updaterDownloadInProgress = true;
        _updaterNoUpdatesMessageCts?.Cancel();
        if (_updaterSelectionPanel is not null)
            _updaterSelectionPanel.IsEnabled = false;
        if (_updaterFoundVersionsSummary is not null)
            _updaterFoundVersionsSummary.Visibility = Visibility.Collapsed;
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
            if (_installedVersionList is not null)
                RenderSwitchVersionList(_installedVersionList, _service.DetectCurrent());
            HideUpdaterSelection();
            ClearCachedUpdaterResults();
            ShowUpdaterReadyState();
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
            _updaterDownloadInProgress = false;
            _updaterCts.Dispose();
            _updaterCts = null;
        }
    }

    private List<string> GetSelectedUpdaterVersions()
    {
        if (_updaterSelectionList is null)
            return [];

        return _updaterSelectionList.Children
            .OfType<Border>()
            .Select(border => border.Child)
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
        foreach (var checkBox in _updaterSelectionList.Children
                     .OfType<Border>()
                     .Select(border => border.Child)
                     .OfType<CheckBox>())
            checkBox.IsChecked = isSelected;
        _cachedUpdaterSelection = GetSelectedUpdaterVersions().ToArray();
        UpdateUpdaterActionButtonState();
    }

    private void UpdaterVersionSelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (_updaterSelectAll is null || _updaterSelectionList is null)
            return;

        var boxes = _updaterSelectionList.Children
            .OfType<Border>()
            .Select(border => border.Child)
            .OfType<CheckBox>()
            .ToList();
        _updaterSelectAll.IsChecked = boxes.Count > 0 && boxes.All(box => box.IsChecked == true);
        _cachedUpdaterSelection = GetSelectedUpdaterVersions().ToArray();
        UpdateUpdaterActionButtonState();
    }

    private void UpdateUpdaterActionButtonState()
    {
        if (_updaterDownloadButton is null || _updaterCancelButton is null)
            return;

        var selectionVisible = _updaterSelectionPanel?.Visibility == Visibility.Visible;
        var hasSelection = GetSelectedUpdaterVersions().Count > 0;
        _updaterDownloadButton.Visibility = selectionVisible && hasSelection
            ? Visibility.Visible
            : Visibility.Collapsed;
        _updaterCancelButton.HorizontalAlignment = selectionVisible && hasSelection
            ? HorizontalAlignment.Left
            : HorizontalAlignment.Center;
    }

    private void CancelUpdaterSelection_Click(object? sender, RoutedEventArgs e)
    {
        ClearCachedUpdaterResults();
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
        if (_updaterFoundVersionsSummary is not null)
        {
            _updaterFoundVersionsSummary.Text = string.Empty;
            _updaterFoundVersionsSummary.Visibility = Visibility.Collapsed;
        }
        UpdateUpdaterActionButtonState();
    }

    private void ShowUpdaterReadyState()
    {
        _pendingUpdaterDownloads = [];
        HideUpdaterSelection();
        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Collapsed;
            _updaterProgress.Value = 0;
        }
        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman ? "Bereit auf Updates zu prüfen" : "Ready to check for updates";
            _updaterVersion.Foreground = (Brush)FindResource("ForegroundBrush");
        }
        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Updateprüfung für neuere Yaca Plugin Versionen"
                : "Check for newer Yaca Plugin versions";
            _updaterStatus.Foreground = (Brush)FindResource("SecondaryBrush");
        }
        if (_updaterSearchButton is not null)
        {
            _updaterSearchButton.Content = IsGerman ? "NACH UPDATES SUCHEN" : "CHECK FOR UPDATES";
            _updaterSearchButton.IsEnabled = true;
        }
        UpdateUpdaterActionButtonState();
    }

    private void UpdateUpdaterProgress(YacaUpdaterProgress progress)
    {
        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = $"YACA {progress.Version}";
            _updaterVersion.Foreground = (Brush)FindResource("ForegroundBrush");
        }
        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = progress.Status;
            _updaterStatus.Foreground = (Brush)FindResource("SecondaryBrush");
        }
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
