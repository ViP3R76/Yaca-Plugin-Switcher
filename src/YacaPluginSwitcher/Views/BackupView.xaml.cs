using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class BackupView : UserControl
{
    private readonly YacaService _service;
    private readonly MainWindow _owner;
    private readonly ObservableCollection<BackupRow> _rows = [];
    private UiText Texts => Localization.Get(_service.Settings.Language);

    public BackupView(YacaService service, MainWindow owner)
    {
        _service = service;
        _owner = owner;
        InitializeComponent();
        LoadBackups();
    }

    private bool SelectiveDeletionEnabled =>
        _service.Settings.ExpertSettings && _service.Settings.SelectableBackupsForDeletion;

    private void LoadBackups()
    {
        TitleText.Text = Texts.BackupTitle;
        _rows.Clear();
        foreach (var backup in _service.Backups.ListBackups())
            _rows.Add(new BackupRow(backup, SelectiveDeletionEnabled));

        Grid.ItemsSource = _rows;

        // The delete action is always available. Its behavior is determined by
        // the selective-deletion setting: selected rows only when enabled,
        // otherwise all backups after confirmation.
        DeleteButton.Visibility = Visibility.Visible;
        DeleteButton.Content = SelectiveDeletionEnabled
            ? (IsGerman() ? "Backups löschen" : "Delete backups")
            : (IsGerman() ? "Alle Backups löschen" : "Delete all backups");
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var selective = SelectiveDeletionEnabled;
        List<BackupInfo> selected;

        if (selective)
        {
            selected = _rows.Where(row => row.Selected).Select(row => row.Info).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(
                    IsGerman() ? "Bitte mindestens ein Backup markieren." : "Please select at least one backup.",
                    Texts.DeleteBackups,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show(
                    Texts.DeleteBackupsQuestion,
                    Texts.DeleteBackups,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        }
        else
        {
            selected = _rows.Select(row => row.Info).ToList();
            if (selected.Count == 0)
                return;

            if (MessageBox.Show(
                    Texts.DeleteAllBackupsQuestion,
                    Texts.DeleteBackups,
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
        }

        try
        {
            _service.Backups.DeleteBackups(selected);
            LoadBackups();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or YacaOperationException)
        {
            _service.Logger.Error($"Backup deletion failed: {ex}");
            MessageBox.Show(
                Localization.GetErrorMessage(ex, Texts, Texts.ErrorUnexpected),
                Texts.DeleteBackups,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Restore_Click(object sender, RoutedEventArgs e)
    {
        if (Grid.SelectedItem is not BackupRow row)
            return;

        var text = Texts;
        if (TeamSpeakDetector.IsRunning())
        {
            MessageBox.Show(text.BackupRunningMessage, text.TeamspeakRunningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show(
                $"{text.RestoreQuestion}\n\n{row.Info.DisplayName}",
                text.Restore,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            var current = _service.DetectCurrent();
            if (current is not null && _service.Backups.CreateBackup(_service.TargetFile, current) is null)
                throw new InvalidOperationException(text.BackupCreatedBeforeRestoreFailed);

            _service.Backups.Restore(row.Info, _service.TargetFile);
            MessageBox.Show(text.SuccessTitle, text.Restore, MessageBoxButton.OK, MessageBoxImage.Information);
            _owner.ReturnHome();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _service.Logger.Error($"Backup restore failed: {ex}");
            MessageBox.Show(
                Localization.GetErrorMessage(ex, text, text.RestoreFailed),
                text.RestoreFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
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

        public BackupRow(BackupInfo info, bool canSelect)
        {
            Info = info;
            CanSelect = canSelect;
        }
    }
}
