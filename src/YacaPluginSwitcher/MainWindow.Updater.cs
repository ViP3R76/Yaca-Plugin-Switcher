using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    /// <summary>
    /// Startet die Suche und den Download fehlender YACA-Versionen.
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

        var progress = new Progress<YacaUpdaterProgress>(UpdateUpdaterProgress);

        try
        {
            var before =
                (await _updater.GetMissingVersionsAsync(_updaterCts.Token)).Count;

            await _updater.DownloadMissingAsync(
                progress,
                _updaterCts.Token);

            await RefreshDownloadedFilesAsync();

            _plugins.Clear();
            _plugins.AddRange(GetDistinctPlugins());

            var after =
                (await _updater.GetMissingVersionsAsync(_updaterCts.Token)).Count;

            ShowSwitchPage();

            SetGlobalStatus(
                before == 0 || after >= before
                    ? (IsGerman
                        ? "Keine neuen YACA Downloads verfügbar"
                        : "No new YACA downloads available")
                    : (IsGerman
                        ? "YACA Downloads aktualisiert."
                        : "YACA downloads refreshed."),
                after < before);
        }
        catch (OperationCanceledException)
        {
            SetGlobalStatus(
                IsGerman
                    ? "YACA Update abgebrochen."
                    : "YACA update cancelled.");
        }
        catch (Exception ex)
        {
            _service.Logger.Error($"YACA updater failed: {ex}");
            SetGlobalStatus(
                IsGerman
                    ? "YACA Update fehlgeschlagen."
                    : "YACA update failed.");
        }
        finally
        {
            _updaterCts.Dispose();
            _updaterCts = null;
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
