using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class BackupView : UserControl
{
    private readonly YacaService _service;
    private readonly MainWindow _owner;
    private readonly ObservableCollection<BackupRow> _rows = [];
    private readonly ObservableCollection<PluginDownloadRow> _pluginDownloadRows = [];
    private bool _pluginDownloadsNewestFirst = true;
    private UiText Texts => Localization.Get(_service.Settings.Language);
    private static string PluginDownloadDirectory => Path.Combine(AppContext.BaseDirectory, "plugins_download");

    public BackupView(YacaService service, MainWindow owner)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        InitializeComponent();
        ApplyLocalizedColumnHeaders();
        LoadBackups();
        LoadPluginDownloads();
    }

    private bool SelectiveDeletionEnabled => _service.Settings.ExpertSettings && _service.Settings.SelectableBackupsForDeletion;

    private void ApplyLocalizedColumnHeaders()
    {
        Grid.Columns[1].Header = SettingsLocalization.Get(_service.Settings.Language, "BackupColumn");
        Grid.Columns[2].Header = SettingsLocalization.Get(_service.Settings.Language, "DateColumn");
        Grid.Columns[3].Header = SettingsLocalization.Get(_service.Settings.Language, "SizeColumn");
        Grid.Columns[4].Header = SettingsLocalization.Get(_service.Settings.Language, "HashColumn");
        PluginDownloadsGrid.Columns[1].Header = SettingsLocalization.Get(_service.Settings.Language, "FileColumn");
        PluginDownloadsGrid.Columns[2].Header = SettingsLocalization.Get(_service.Settings.Language, "DateColumn");
        PluginDownloadsGrid.Columns[3].Header = SettingsLocalization.Get(_service.Settings.Language, "SizeColumn");
        PluginDownloadsGrid.Columns[4].Header = SettingsLocalization.Get(_service.Settings.Language, "VersionColumn");
    }

    private void LoadBackups()
    {
        PageHeaderText.Text = IsGerman() ? "BACKUPS VERWALTEN" : "MANAGE BACKUPS";
        TitleText.Text = Texts.BackupTitle;
        BackupSectionHeader.Text = IsGerman() ? "YACA Plugin Backups" : "YACA Plugin Backups";
        BackupReplacementText.Text = SettingsLocalization.Get(_service.Settings.Language, "BackupReplacementNotice");
        RestoreButton.Content = Texts.Restore;
        CloseButton.Content = Texts.Close;
        DeleteButton.Content = Texts.Delete.ToUpperInvariant();
        ApplyLocalizedColumnHeaders();

        _rows.Clear();
        foreach (var backup in _service.Backups.ListBackups().OrderByDescending(backup => backup.Timestamp))
            _rows.Add(new BackupRow(backup, SelectiveDeletionEnabled));

        Grid.ItemsSource = _rows;
        BackupCapacityText.Text = $"Backups: {_rows.Count} / {_service.Settings.MaxBackups}";

        UpdateBackupPanelLayout(_service.Settings.KeepYacaPluginDownloads);
        DeleteButton.Visibility = Visibility.Visible;
    }

    private void UpdateBackupPanelLayout(bool pluginDownloadsEnabled)
    {
        BackupPanelRow.Height = pluginDownloadsEnabled
            ? GridLength.Auto
            : new GridLength(1, GridUnitType.Star);
        PluginDownloadsPanelRow.Height = pluginDownloadsEnabled
            ? new GridLength(1, GridUnitType.Star)
            : new GridLength(0);

        if (BackupCard.Parent is Grid outerGrid && outerGrid.RowDefinitions.Count > 1)
            outerGrid.RowDefinitions[1].Height = pluginDownloadsEnabled ? new GridLength(12) : new GridLength(0);

        if (BackupCard.Child is Grid backupGrid && backupGrid.RowDefinitions.Count > 2)
            backupGrid.RowDefinitions[2].Height = pluginDownloadsEnabled
                ? GridLength.Auto
                : new GridLength(1, GridUnitType.Star);

        BackupCard.Height = pluginDownloadsEnabled
            ? 99 + Math.Max(1, _service.Settings.MaxBackups) * 44
            : double.NaN;
    }

    private void LoadPluginDownloads()
    {
        var enabled = _service.Settings.KeepYacaPluginDownloads;
        PluginDownloadsCard.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        UpdateBackupPanelLayout(enabled);

        if (!enabled)
            return;

        PluginDownloadsTitle.Text = IsGerman() ? "YACA Plugin Downloads" : "YACA Plugin Downloads";

        _pluginDownloadRows.Clear();
        Directory.CreateDirectory(PluginDownloadDirectory);

        var files = Directory.EnumerateFiles(PluginDownloadDirectory, "*.ts3_plugin", SearchOption.TopDirectoryOnly)
            .Select(file => new PluginDownloadRow(file))
            .OrderByDescending(row => ParseVersion(row.Version));

        var orderedFiles = _pluginDownloadsNewestFirst ? files : files.Reverse();
        foreach (var row in orderedFiles)
            _pluginDownloadRows.Add(row);

        PluginDownloadsGrid.ItemsSource = _pluginDownloadRows;
    }

    private void PluginDownloadsSort_Click(object sender, RoutedEventArgs e)
    {
        _pluginDownloadsNewestFirst = !_pluginDownloadsNewestFirst;
        LoadPluginDownloads();
    }

    private static Version ParseVersion(string version) => Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0, 0);

    private void BackupSelection_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not BackupRow row)
            return;
        row.Selected = checkBox.IsChecked == true;
    }

    private void PluginDownloadSelection_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.DataContext is not PluginDownloadRow row)
            return;
        row.Selected = checkBox.IsChecked == true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selectedBackups = _rows.Where(row => row.Selected).Select(row => row.Info).ToList();
        var selectedDownloads = _pluginDownloadRows.Where(row => row.Selected).ToList();
        var backupsToDelete = SelectiveDeletionEnabled ? selectedBackups : _rows.Select(row => row.Info).ToList();

        if (backupsToDelete.Count == 0 && selectedDownloads.Count == 0)
        {
            SetFooter(SelectiveDeletionEnabled
                ? (IsGerman() ? "Bitte mindestens ein Backup oder einen Plugin-Download markieren." : "Please select at least one backup or plugin download.")
                : (IsGerman() ? "Keine Backups oder Plugin-Downloads zum Löschen vorhanden." : "No backups or plugin downloads available for deletion."), false);
            return;
        }

        var deletedBackups = backupsToDelete.Count > 0 ? DeleteBackups(backupsToDelete, false) : 0;
        var deletedDownloads = selectedDownloads.Count > 0 ? DeletePluginDownloads(selectedDownloads, false) : 0;
        var parts = new List<string>();
        if (deletedBackups > 0) parts.Add(IsGerman() ? $"{deletedBackups} Backup(s)" : $"{deletedBackups} backup(s)");
        if (deletedDownloads > 0) parts.Add(IsGerman() ? $"{deletedDownloads} Plugin-Download(s)" : $"{deletedDownloads} plugin download(s)");

        SetFooter(parts.Count > 0
            ? (IsGerman() ? string.Join(" und ", parts) + " wurden gelöscht." : string.Join(" and ", parts) + " deleted.")
            : (IsGerman() ? "Keine ausgewählten Einträge konnten gelöscht werden." : "No selected entries could be deleted."), parts.Count > 0);
    }

    private int DeleteBackups(List<BackupInfo> backups, bool updateFooter = true)
    {
        try
        {
            _service.Backups.DeleteBackups(backups);
            LoadBackups();
            if (updateFooter) SetFooter(IsGerman() ? $"{backups.Count} Backup(s) wurden gelöscht." : $"{backups.Count} backup(s) deleted.", true);
            return backups.Count;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or YacaOperationException)
        {
            _service.Logger.Error($"Backup deletion failed: {ex}");
            if (updateFooter) SetFooter(Localization.GetErrorMessage(ex, Texts, Texts.ErrorUnexpected), false);
            return 0;
        }
    }

    private int DeletePluginDownloads(List<PluginDownloadRow> downloads, bool updateFooter = true)
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
        if (updateFooter) SetFooter(IsGerman() ? $"{deleted} Plugin-Download(s) wurden gelöscht." : $"{deleted} plugin download(s) deleted.", deleted > 0);
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
            LoadBackups();
            LoadPluginDownloads();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or YacaOperationException)
        {
            _service.Logger.Error($"Backup restore failed: {ex}");
            SetFooter(Localization.GetErrorMessage(ex, text, text.RestoreFailed), false);
        }
    }

    private void SetFooter(string message, bool success)
    {
        var footer = _owner.FindName("GlobalFooterStatusText") as TextBlock;
        if (footer is null) return;
        footer.Text = message;
        footer.Foreground = (Brush)_owner.FindResource(success ? "SuccessBrush" : "ForegroundBrush");
        footer.FontWeight = success ? FontWeights.Bold : FontWeights.Normal;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _owner.ReturnHome();
    private bool IsGerman() => Localization.Normalize(_service.Settings.Language) == Localization.German;

    private sealed class BackupRow : INotifyPropertyChanged
    {
        public BackupInfo Info { get; }
        private bool _selected;
        public bool Selected { get => _selected; set { if (_selected == value) return; _selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected))); } }
        public bool CanSelect { get; }
        public string DisplayName => Info.DisplayName;
        public DateTime Timestamp => Info.Timestamp;
        public string Sha256 => Info.Sha256;
        public string FileSizeDisplay => $"{Info.FileSize / 1024d / 1024d:0.00} MB";
        public event PropertyChangedEventHandler? PropertyChanged;
        public BackupRow(BackupInfo info, bool canSelect) { Info = info ?? throw new ArgumentNullException(nameof(info)); CanSelect = canSelect; }
    }

    private sealed class PluginDownloadRow : INotifyPropertyChanged
    {
        public string FilePath { get; }
        public string FileName { get; }
        public string Version { get; }
        public string BuildVersion { get; }
        public DateTime Timestamp { get; }
        public long FileSize { get; }
        public string FileSizeDisplay => $"{FileSize / 1024d / 1024d:0.00} MB";
        private bool _selected;
        public bool Selected { get => _selected; set { if (_selected == value) return; _selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
        public PluginDownloadRow(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Version = ExtractVersion(FileName) ?? "—";
            BuildVersion = ExtractBuildVersion(FileName) ?? "—";
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
        private static string? ExtractBuildVersion(string fileName)
        {
            var match = System.Text.RegularExpressions.Regex.Match(fileName, @"yaca_\d+_(?<build>[^.]+(?:\.[^.]+)*?)\.ts3_plugin$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["build"].Value : null;
        }
    }
}
