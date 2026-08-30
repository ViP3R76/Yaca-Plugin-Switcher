using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private CheckBox? _updaterSelectAll;
    private Button? _updaterSearchButton;
    private Button? _updaterDownloadButton;
    private StackPanel? _updaterSelectionList;
    private StackPanel? _updaterSelectionPanel;

    /// <summary>
    /// Startet die Updateprüfung. Nach der Prüfung wird bewusst noch kein Download
    /// gestartet. Der Benutzer erhält zunächst eine Auswahl der tatsächlich fehlenden
    /// Versionen und entscheidet selbst über eine, mehrere oder alle Versionen.
    /// </summary>
    private async Task RunUpdaterAsync()
    {
        if (_updaterCts is not null)
        {
            return;
        }

        _updaterCts = new CancellationTokenSource();

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

            var missingVersions = await _updater.GetMissingVersionsAsync(_updaterCts.Token);

            if (missingVersions.Count == 0)
            {
                SetGlobalStatus(
                    IsGerman
                        ? "Keine neuen YACA Downloads verfügbar"
                        : "No new YACA downloads available");
                ShowUpdaterReadyState();
                return;
            }

            ShowUpdaterSelection(missingVersions);
        }
        catch (OperationCanceledException)
        {
            SetGlobalStatus(
                IsGerman
                    ? "YACA Updateprüfung abgebrochen."
                    : "YACA update check cancelled.");
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"YACA updater check failed: {ex}");
            SetGlobalStatus(
                IsGerman
                    ? "YACA Updateprüfung fehlgeschlagen."
                    : "YACA update check failed.");
        }
        finally
        {
            _updaterCts.Dispose();
            _updaterCts = null;
        }
    }

    /// <summary>
    /// Erstellt die kompakte Auswahloberfläche einmalig.
    /// </summary>
    private void EnsureUpdaterSelectionControls()
    {
        if (_updaterSelectionPanel is not null)
        {
            ResolveUpdaterSearchButton();
            return;
        }

        if (_updaterStatus?.Parent is not StackPanel parent)
        {
            return;
        }

        _updaterSelectionList = new StackPanel
        {
            Margin = new Thickness(6, 2, 6, 2)
        };

        var versionScroll = new ScrollViewer
        {
            Content = _updaterSelectionList,
            MaxHeight = 118,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
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

        var downloadButton = new Button
        {
            Content = IsGerman ? "AUSGEWÄHLTE HERUNTERLADEN" : "DOWNLOAD SELECTED",
            Height = 38,
            Style = (Style)FindResource("NormalActionButtonStyle"),
            Margin = new Thickness(0, 6, 4, 0)
        };
        downloadButton.Click += async (_, _) => await DownloadSelectedUpdaterVersionsAsync();
        _updaterDownloadButton = downloadButton;

        var cancelButton = new Button
        {
            Content = IsGerman ? "ABBRECHEN" : "CANCEL",
            Height = 38,
            Style = (Style)FindResource("NormalActionButtonStyle"),
            Margin = new Thickness(4, 6, 0, 0)
        };
        cancelButton.Click += CancelUpdaterSelection_Click;

        var buttonGrid = new Grid();
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
        buttonGrid.ColumnDefinitions.Add(new ColumnDefinition());
        Grid.SetColumn(downloadButton, 0);
        Grid.SetColumn(cancelButton, 1);
        buttonGrid.Children.Add(downloadButton);
        buttonGrid.Children.Add(cancelButton);

        _updaterSelectionPanel = new StackPanel
        {
            Visibility = Visibility.Collapsed,
            Background = (Brush)FindResource("ControlBrush"),
            Margin = new Thickness(0, 8, 0, 0)
        };

        _updaterSelectionPanel.Children.Add(
            new Border
            {
                Height = 1,
                Background = (Brush)FindResource("AccentSoftBrush"),
                Margin = new Thickness(0, 0, 0, 5)
            });
        _updaterSelectionPanel.Children.Add(
            new TextBlock
            {
                Text = IsGerman ? "DOWNLOAD AUSWAHL" : "DOWNLOAD SELECTION",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("GoldBrush"),
                Margin = new Thickness(4, 0, 4, 0)
            });
        _updaterSelectionPanel.Children.Add(_updaterSelectAll);
        _updaterSelectionPanel.Children.Add(versionScroll);
        _updaterSelectionPanel.Children.Add(buttonGrid);

        parent.Children.Add(_updaterSelectionPanel);
        ResolveUpdaterSearchButton();
    }

    /// <summary>
    /// Ermittelt den zentral gestylten Updateprüfungsbutton der aktuellen Seite.
    /// </summary>
    private void ResolveUpdaterSearchButton()
    {
        if (PageHost.Content is not Grid root)
        {
            return;
        }

        _updaterSearchButton = FindVisualChildren<Button>(root)
            .FirstOrDefault(button =>
                button.Content is string text
                && (text.Contains("NACH UPDATES SUCHEN", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("CHECK FOR UPDATES", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Zeigt die gefundenen fehlenden Versionen zur expliziten Auswahl an.
    /// </summary>
    private void ShowUpdaterSelection(IReadOnlyList<string> versions)
    {
        EnsureUpdaterSelectionControls();

        if (_updaterSelectionPanel is null || _updaterSelectionList is null)
        {
            return;
        }

        _updaterSelectionList.Children.Clear();

        foreach (var version in versions)
        {
            var checkBox = new CheckBox
            {
                Content = $"YACA {version}",
                IsChecked = true,
                FontSize = 13,
                Margin = new Thickness(4, 2, 4, 2),
                Foreground = (Brush)FindResource("ForegroundBrush")
            };

            _updaterSelectionList.Children.Add(checkBox);
        }

        if (_updaterSelectAll is not null)
        {
            _updaterSelectAll.IsChecked = true;
        }

        _updaterSelectionPanel.Visibility = Visibility.Visible;
        ResolveUpdaterSearchButton();

        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman
                ? $"{versions.Count} Updates gefunden"
                : $"{versions.Count} updates found";
        }

        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Wähle eine, mehrere oder alle Versionen für den Download."
                : "Select one, multiple or all versions to download.";
        }

        if (_updaterSearchButton is not null)
        {
            _updaterSearchButton.IsEnabled = false;
        }
    }

    /// <summary>
    /// Lädt die vom Benutzer ausgewählten Versionen herunter und integriert sie
    /// über denselben zentralen Validierungs- und Installationspfad wie bisher.
    /// </summary>
    private async Task DownloadSelectedUpdaterVersionsAsync()
    {
        var selectedVersions = GetSelectedUpdaterVersions();

        if (selectedVersions.Count == 0)
        {
            SetGlobalStatus(
                IsGerman
                    ? "Bitte mindestens eine YACA Version auswählen."
                    : "Please select at least one YACA version.");
            return;
        }

        if (_updaterCts is not null)
        {
            return;
        }

        _updaterCts = new CancellationTokenSource();

        if (_updaterSelectionPanel is not null)
        {
            _updaterSelectionPanel.IsEnabled = false;
        }

        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Visible;
            _updaterProgress.Value = 0;
        }

        if (_updaterDownloadButton is not null)
        {
            _updaterDownloadButton.IsEnabled = false;
        }

        var progress = new Progress<YacaUpdaterProgress>(UpdateUpdaterProgress);

        try
        {
            await _updater.DownloadSelectedAsync(
                selectedVersions,
                progress,
                _updaterCts.Token);

            await RefreshDownloadedFilesAsync();

            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());

            HideUpdaterSelection();
            ShowSwitchPage();

            SetGlobalStatus(
                IsGerman
                    ? "YACA Downloads aktualisiert."
                    : "YACA downloads refreshed.",
                true);
        }
        catch (OperationCanceledException)
        {
            SetGlobalStatus(
                IsGerman
                    ? "YACA Download abgebrochen."
                    : "YACA download cancelled.");
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"YACA updater download failed: {ex}");
            SetGlobalStatus(
                IsGerman
                    ? "YACA Download fehlgeschlagen."
                    : "YACA download failed.");
        }
        finally
        {
            if (_updaterSelectionPanel is not null)
            {
                _updaterSelectionPanel.IsEnabled = true;
            }

            if (_updaterDownloadButton is not null)
            {
                _updaterDownloadButton.IsEnabled = true;
            }

            _updaterCts.Dispose();
            _updaterCts = null;
        }
    }

    /// <summary>
    /// Liest die aktuell markierten Versionen aus der Auswahl.
    /// </summary>
    private List<string> GetSelectedUpdaterVersions()
    {
        if (_updaterSelectionList is null)
        {
            return [];
        }

        return _updaterSelectionList.Children
            .OfType<CheckBox>()
            .Where(checkBox => checkBox.IsChecked == true)
            .Select(checkBox => checkBox.Content?.ToString())
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Select(version => version!.Replace("YACA ", "", StringComparison.OrdinalIgnoreCase).Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Schaltet die Markierung aller gefundenen Versionen gemeinsam um.
    /// </summary>
    private void UpdaterSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        if (_updaterSelectAll is null || _updaterSelectionList is null)
        {
            return;
        }

        var isSelected = _updaterSelectAll.IsChecked == true;

        foreach (var checkBox in _updaterSelectionList.Children.OfType<CheckBox>())
        {
            checkBox.IsChecked = isSelected;
        }
    }

    /// <summary>
    /// Bricht die Auswahl ab, ohne einen Download auszuführen.
    /// </summary>
    private void CancelUpdaterSelection_Click(object? sender, RoutedEventArgs e)
    {
        HideUpdaterSelection();
        ShowUpdaterReadyState();
        SetGlobalStatus(
            IsGerman
                ? "YACA Downloadauswahl verworfen."
                : "YACA download selection cancelled.");
    }

    /// <summary>
    /// Setzt das Updater-Panel auf den normalen Ausgangszustand zurück.
    /// </summary>
    private void HideUpdaterSelection()
    {
        if (_updaterSelectionPanel is not null)
        {
            _updaterSelectionPanel.Visibility = Visibility.Collapsed;
        }

        if (_updaterSelectionList is not null)
        {
            _updaterSelectionList.Children.Clear();
        }

        if (_updaterSelectAll is not null)
        {
            _updaterSelectAll.IsChecked = false;
        }

        if (_updaterSearchButton is not null)
        {
            _updaterSearchButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Setzt die Statusanzeige auf den Bereitschaftszustand zurück.
    /// </summary>
    private void ShowUpdaterReadyState()
    {
        HideUpdaterSelection();

        if (_updaterProgress is not null)
        {
            _updaterProgress.Visibility = Visibility.Collapsed;
            _updaterProgress.Value = 0;
        }

        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = IsGerman
                ? "Bereit für Updates"
                : "Ready for updates";
        }

        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = IsGerman
                ? "Neue YACA Versionen können hier heruntergeladen werden."
                : "New YACA versions can be downloaded here.";
        }

        if (_updaterSearchButton is not null)
        {
            _updaterSearchButton.IsEnabled = true;
        }
    }

    /// <summary>
    /// Aktualisiert Fortschrittsanzeige und Status des Updaters.
    /// </summary>
    private void UpdateUpdaterProgress(YacaUpdaterProgress progress)
    {
        if (_updaterVersion is not null)
        {
            _updaterVersion.Text = $"YACA {progress.Version}";
        }

        if (_updaterStatus is not null)
        {
            _updaterStatus.Text = progress.Status;
        }

        if (_updaterProgress is not null && progress.TotalBytes is > 0)
        {
            _updaterProgress.Value = Math.Min(
                100,
                progress.BytesReceived * 100d / progress.TotalBytes.Value);
        }

        if (_updaterSize is not null)
        {
            _updaterSize.Text = progress.TotalBytes is > 0
                ? $"{progress.BytesReceived / 1024d / 1024d:0.00} MB / " +
                  $"{progress.TotalBytes.Value / 1024d / 1024d:0.00} MB"
                : string.Empty;
        }

        if (!progress.Completed)
        {
            return;
        }

        if (progress.Success)
        {
            SetGlobalStatus(
                $"YACA {progress.Version}: {progress.Status}",
                true);
        }
        else
        {
            SetGlobalStatus(
                $"YACA {progress.Version}: {progress.Status}");
        }
    }

    /// <summary>
    /// Aktualisiert die Liste der lokal vorhandenen Downloads.
    /// </summary>
    private async Task RefreshDownloadedFilesAsync()
    {
        if (_downloadedFilesPanel is null)
        {
            return;
        }

        var files = await _updater.GetAvailableDownloadsAsync();
        _downloadedFilesPanel.Children.Clear();

        if (files.Count == 0)
        {
            _downloadedFilesPanel.Children.Add(
                new TextBlock
                {
                    Text = IsGerman
                        ? "Noch keine Downloads vorhanden."
                        : "No downloads yet.",
                    Foreground = (Brush)FindResource("SecondaryBrush"),
                    FontSize = 14
                });

            return;
        }

        foreach (var file in files)
        {
            var row = new Grid
            {
                MinHeight = 38,
                Margin = new Thickness(0, 2, 0, 2)
            };

            row.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            row.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });

            row.Children.Add(
                new TextBlock
                {
                    Text = $"YACA {file.Version}",
                    FontSize = 15,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });

            var size = new TextBlock
            {
                Text = $"{file.Size / 1024d / 1024d:0.00} MB",
                FontSize = 13,
                Foreground = (Brush)FindResource("SecondaryBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(size, 1);
            row.Children.Add(size);
            _downloadedFilesPanel.Children.Add(row);
        }
    }
}
