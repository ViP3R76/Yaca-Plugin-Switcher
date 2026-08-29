using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class BackupView : UserControl
{
    private readonly YacaService _service;
    private readonly MainWindow _owner;
    private readonly ObservableCollection<BackupRow> _rows = [];
    private readonly ObservableCollection<PluginDownloadRow> _pluginDownloadRows = [];
    private UiText Texts => Localization.Get(_service.Settings.Language);
    private static string PluginDownloadDirectory => Path.Combine(AppContext.BaseDirectory, "plugins_download");

    public BackupView(YacaService service, MainWindow owner)
    {
        _service = service;
        _owner = owner;
        InitializeComponent();
        LoadBackups();
        LoadPluginDownloads();
    }

    private bool SelectiveDeletionEnabled =>
        _service.Settings.ExpertSettings && _service.Settings.SelectableBackupsForDeletion;

    private void LoadBackups()
    {
        TitleText.Text = Texts.BackupTitle;
        _rows.Clear();
        var backups = _service.Backups.ListBackups();
        foreach (var backup in backups)
            _rows.Add(new BackupRow(backup, SelectiveDeletionEnabled));

        Grid.ItemsSource = _rows;
        BackupCapacityText.Text = $"Backups: {_rows.Count} / {_service.Settings.MaxBackups}";
        BackupCard.Height = 54 + Math.Max(1, _service.Settings.MaxBackups) * 44;

        DeleteButton.Visibility = Visibility.Visible;
        DeleteButton.Content = SelectiveDeletionEnabled
            ? (IsGerman() ? "Backups löschen" : "Delete backups")
            : (IsGerman() ? "Alle Backups löschen" : "Delete all backups");
    }

    private void LoadPluginDownloads()
    {
        var enabled = _service.Settings.KeepYacaPluginDownloads;
        PluginDownloadsCard.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        if (!enabled)
            return;

        PluginDownloadsTitle.Text = IsGerman() ? "YACA Plugin Downloads" : "YACA Plugin Downloads";
        _pluginDownloadRows.Clear();
        Directory.CreateDirectory(PluginDownloadDirectory);
        foreach (var file in Directory.EnumerateFiles(PluginDownloadDirectory, "*.ts3_plugin", SearchOption.TopDirectoryOnly)
                     .OrderByDescending(File.GetLastWriteTime))
            _pluginDownloadRows.Add(new PluginDownloadRow(file));

        PluginDownloadsGrid.ItemsSource = _pluginDownloadRows;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedBackups = _rows.Where(row => row.Selected).Select(row => row.Info).ToList();
        var selectedDownloads = _pluginDownloadRows.Where(row => row.Selected).ToList();

        if (selectedDownloads.Count > 0 && selectedBackups.Count == 0)
        {
            DeletePluginDownloads(selectedDownloads);
            return;
        }

        if (selectedDownloads.Count == 0 && SelectiveDeletionEnabled && selectedBackups.Count > 0)
        {
            DeleteBackups(selectedBackups);
            return;
        }

        if (selectedDownloads.Count > 0 && selectedBackups.Count > 0)
        {
            var deletedDownloads = DeletePluginDownloads(selectedDownloads, false);
            var deletedBackups = DeleteBackups(selectedBackups, false);
            SetFooter(IsGerman()
                ? $"{deletedBackups} Backup(s) und {deletedDownloads} Plugin-Download(s) wurden gelöscht."
                : $"{deletedBackups} backup(s) and {deletedDownloads} plugin download(s) deleted.",
                deletedBackups > 0 || deletedDownloads > 0);
            return;
        }

        if (!SelectiveDeletionEnabled)
        {
            var allBackups = _rows.Select(row => row.Info).ToList();
            if (allBackups.Count == 0)
            {
                SetFooter(IsGerman() ? "Keine Backups vorhanden." : "No backups available.", false);
                return;
            }
            DeleteBackups(allBackups);
            return;
        }

        SetFooter(IsGerman() ? "Bitte mindestens ein Backup oder einen Plugin-Download markieren." : "Please select at least one backup or plugin download.", false);
    }

    private int DeleteBackups(List<BackupInfo> backups, bool updateFooter = true)
    {
        try
        {
            _service.Backups.DeleteBackups(backups);
            LoadBackups();
            if (updateFooter)
                SetFooter(IsGerman() ? $"{backups.Count} Backup(s) wurden gelöscht." : $"{backups.Count} backup(s) deleted.", true);
            return backups.Count;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or YacaOperationException)
        {
            _service.Logger.Error($"Backup deletion failed: {ex}");
            if (updateFooter)
                SetFooter(Localization.GetErrorMessage(ex, Texts, Texts.ErrorUnexpected), false);
            return 0;
        }
    }

    private int DeletePluginDownloads(IReadOnlyList<PluginDownloadRow> downloads, bool updateFooter = true)
    {
        var deleted = 0;
        foreach (var row in downloads)
        {
            try
            {
                if (File.Exists(row.FilePath))
                {
                    File.Delete(row.FilePath);
                    deleted++;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _service.Logger.Error($"Plugin download deletion failed: {ex}");
            }
        }

        LoadPluginDownloads();
        if (updateFooter)
            SetFooter(IsGerman() ? $"{deleted} Plugin-Download(s) wurden gelöscht." : $"{deleted} plugin download(s) deleted.", deleted > 0);
        return deleted;
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not BackupRow row)
        {
            SetFooter(IsGerman() ? "Bitte ein Backup auswählen." : "Please select a backup.", false);
            return;
        }

        var text = Texts;
        if (TeamSpeakDetector.IsRunning())
        {
            SetFooter(text.BackupRunningMessage, false);
            return;
        }

        try
        {
            var current = _service.DetectCurrent();
            if (current is not null && _service.Backups.CreateBackup(_service.TargetFile, current) is null)
                throw new InvalidOperationException(text.BackupCreatedBeforeRestoreFailed);

            _service.Backups.Restore(row.Info, _service.TargetFile);
            SetFooter(IsGerman() ? "Backup wurde erfolgreich wiederhergestellt." : "Backup was restored successfully.", true);
            _owner.ReturnHome();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException)
        {
            _service.Logger.Error($"Backup restore failed: {ex}");
            SetFooter(Localization.GetErrorMessage(ex, text, text.RestoreFailed), false);
        }
    }

    private void SetFooter(string message, bool success)
    {
        if (_owner.FindName("GlobalFooterStatusText") is TextBlock footer)
        {
            footer.Text = message;
            footer.Foreground = (Brush)_owner.FindResource(success ? "SuccessBrush" : "ForegroundBrush");
            footer.FontWeight = success ? FontWeights.Bold : FontWeights.Normal;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _owner.ReturnHome();
    private bool IsGerman() => Localization.Normalize(_service.Settings.Language) == Localization.German;

    private sealed class BackupRow
    {
        public BackupInfo Info { get; }
        public bool Selected { get; set; }
        public bool CanSelect { get; }
        public string DisplayName => Info.DisplayName;
        public DateTime Timestamp => Info.Timestamp;
        public string Sha256 => Info.Sha256;
        public string FileSizeDisplay => $"{Info.FileSize / 1024d / 1024d:0.00} MB";
        public BackupRow(BackupInfo info, bool canSelect) { Info = info; CanSelect = canSelect; }
    }

    private sealed class PluginDownloadRow
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string Version { get; }
        public DateTime Timestamp { get; }
        public long FileSize { get; }
        public string FileSizeDisplay => $"{FileSize / 1024d / 1024d:0.00} MB";
        public bool Selected { get; set; }

        public PluginDownloadRow(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Version = ExtractVersion(FileName) ?? "—";
            Timestamp = File.GetLastWriteTime(filePath);
            FileSize = new FileInfo(filePath).Length;
        }

        private static string? ExtractVersion(string fileName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"yaca_(\d+)(?:_3\.6\.x)?\.ts3_plugin", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!match.Success || match.Groups[1].Value.Length != 3) return null;
            var digits = match.Groups[1].Value;
            return $"{digits[0]}.{digits[1]}.{digits[2]}";
        }
    }
}
