using YacaPluginSwitcher.Configuration;
using System.Globalization;
using System.Reflection;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher.UI;

public sealed class BackupForm : Form
{
    private const int InitialWidth = 1040;
    private const int MinimumWidth = 760;
    private const int MinimumHeight = 360;
    private const int MaximumListHeight = 560;
    private const int CellPadding = 18;
    private const int DeleteColumnWidth = 64;

    private readonly YacaService _service;
    private readonly string _language;
    private readonly DarkBackupGrid _grid = new();
    private Button? _deleteButton;
    private UiText Texts => Localization.Get(_language);
    private bool IndividualDeletionEnabled => _service.Settings.ExpertSettings && _service.Settings.SelectableBackupsForDeletion;

    public BackupForm(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _language = Localization.Normalize(service.Settings.Language);
        var text = Localization.Get(_language);

        Text = text.BackupTitle;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = false;
        ClientSize = new Size(InitialWidth, MinimumHeight);
        MinimumSize = new Size(MinimumWidth, MinimumHeight);
        MaximumSize = new Size(Screen.FromControl(this).WorkingArea.Width, GetMaximumFormHeight());
        Font = new Font("Segoe UI", 10F);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        DarkMode.Apply(this);

        BuildUi(text);
        LoadBackups();
        Resize += (_, _) => UpdateGridViewport();
    }

    private void BuildUi(UiText text)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(root);

        ConfigureGrid(text);
        root.Controls.Add(_grid, 0, 0);

        var buttonsHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Theme.Background
        };
        buttonsHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        buttonsHost.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttonsHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Background,
            Padding = new Padding(0, 4, 0, 0)
        };

        var restore = MakeButton(text.Restore, 150);
        restore.Click += (_, _) => RestoreSelected();
        var close = MakeButton(text.Close, 110);
        close.Click += (_, _) => Close();
        buttons.Controls.Add(restore);
        buttons.Controls.Add(close);
        buttonsHost.Controls.Add(buttons, 1, 0);

        _deleteButton = MakeButton(text.DeleteBackups, 150);
        _deleteButton.BackColor = Theme.Error;
        _deleteButton.FlatAppearance.BorderColor = Theme.Error;
        _deleteButton.Click += (_, _) => DeleteBackups();
        var deleteHost = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background };
        _deleteButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        deleteHost.Controls.Add(_deleteButton);
        deleteHost.Resize += (_, _) => _deleteButton.Location = new Point(Math.Max(0, deleteHost.ClientSize.Width - _deleteButton.Width), 2);
        buttonsHost.Controls.Add(deleteHost, 2, 0);
        root.Controls.Add(buttonsHost, 0, 1);
    }

    private void ConfigureGrid(UiText text)
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = false;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AllowUserToResizeColumns = false;
        _grid.RowHeadersVisible = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _grid.ColumnHeadersHeight = 30;
        _grid.RowTemplate.Height = 30;
        _grid.EnableHeadersVisualStyles = false;
        _grid.BackgroundColor = Theme.Surface;
        _grid.GridColor = Color.FromArgb(64, 68, 76);
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Control,
            ForeColor = Theme.Foreground,
            SelectionBackColor = Theme.Control,
            SelectionForeColor = Theme.Foreground,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0)
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            SelectionBackColor = Theme.ControlHover,
            SelectionForeColor = Theme.Foreground,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(6, 0, 6, 0)
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface,
            ForeColor = Theme.Foreground,
            SelectionBackColor = Theme.ControlHover,
            SelectionForeColor = Theme.Foreground
        };

        _grid.Columns.Clear();
        if (IndividualDeletionEnabled)
        {
            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Delete",
                HeaderText = text.Delete,
                Width = DeleteColumnWidth,
                MinimumWidth = DeleteColumnWidth,
                ReadOnly = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FlatStyle = FlatStyle.Standard,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Theme.Surface,
                    ForeColor = Theme.Foreground,
                    SelectionBackColor = Theme.ControlHover,
                    SelectionForeColor = Theme.Foreground
                }
            });
        }

        AddTextColumn("Date", text.Date);
        AddTextColumn("Yaca", text.Yaca);
        AddTextColumn("Size", text.Size);
        AddTextColumn("Hash", text.Hash);
    }

    private void AddTextColumn(string name, string header)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            MinimumWidth = 40
        });
    }

    private static Button MakeButton(string text, int width)
    {
        var button = new Button { Text = text, Width = width, Height = 36, Margin = new Padding(5, 2, 5, 2) };
        Theme.StyleButton(button);
        return button;
    }

    private void LoadBackups()
    {
        var text = Localization.Get(_language);
        _grid.SuspendLayout();
        try
        {
            _grid.Rows.Clear();
            var backups = _service.Backups.ListBackups();
            foreach (var backup in backups)
            {
                var row = _grid.Rows[_grid.Rows.Add()];
                var offset = IndividualDeletionEnabled ? 1 : 0;
                if (IndividualDeletionEnabled)
                    row.Cells[0].Value = false;

                row.Cells[offset].Value = backup.Timestamp.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                row.Cells[offset + 1].Value = backup.DisplayName;
                row.Cells[offset + 2].Value = $"{backup.FileSize:N0} Bytes";
                row.Cells[offset + 3].Value = string.IsNullOrWhiteSpace(backup.Sha256) ? "-" : backup.Sha256;
                row.Tag = backup;
            }

            if (backups.Count == 0)
            {
                var row = _grid.Rows[_grid.Rows.Add()];
                var messageColumn = IndividualDeletionEnabled ? 1 : 0;
                if (IndividualDeletionEnabled)
                    row.Cells[0].Value = false;

                row.Cells[messageColumn].Value = text.NoBackups;
                row.Cells[messageColumn].Style.ForeColor = Theme.SecondaryForeground;
                row.Tag = null;
                for (var i = messageColumn + 1; i < _grid.Columns.Count; i++)
                    row.Cells[i].Value = string.Empty;
            }
        }
        finally
        {
            _grid.ResumeLayout();
        }

        FitColumnsToContent();
        ResizeToContent();
    }

    private void FitColumnsToContent()
    {
        if (_grid.Columns.Count == 0)
            return;

        var naturalWidths = new int[_grid.Columns.Count];
        for (var columnIndex = 0; columnIndex < _grid.Columns.Count; columnIndex++)
        {
            if (_grid.Columns[columnIndex] is DataGridViewCheckBoxColumn)
            {
                naturalWidths[columnIndex] = DeleteColumnWidth;
                continue;
            }

            var headerWidth = TextRenderer.MeasureText(_grid.Columns[columnIndex].HeaderText, _grid.Font).Width;
            var width = headerWidth + CellPadding;
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.Cells[columnIndex].Value is null)
                    continue;

                var text = Convert.ToString(row.Cells[columnIndex].Value, CultureInfo.InvariantCulture) ?? string.Empty;
                width = Math.Max(width, TextRenderer.MeasureText(text, _grid.Font).Width + CellPadding);
            }

            naturalWidths[columnIndex] = Math.Max(48, width);
        }

        for (var i = 0; i < naturalWidths.Length; i++)
            _grid.Columns[i].Width = naturalWidths[i];

        var requiredClientWidth = naturalWidths.Sum() + SystemInformation.VerticalScrollBarWidth + 4;
        var requiredFormWidth = Math.Max(MinimumWidth, requiredClientWidth + 36);
        MinimumSize = new Size(requiredFormWidth, MinimumHeight);
        UpdateGridViewport();
    }

    private void UpdateGridViewport()
    {
        if (_grid.ClientSize.Width <= 0)
            return;

        // The columns are intentionally content-sized. Never stretch or shrink them
        // merely because the user resizes the form; unused space stays empty.
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _grid.ScrollBars = ScrollBars.Vertical;
    }

    private void ResizeToContent()
    {
        var rowHeight = Math.Max(26, _grid.Font.Height + 10);
        var actualRows = _grid.Rows.Count == 1 && _grid.Rows[0].Tag is null ? 1 : _grid.Rows.Count;
        var visibleRows = Math.Clamp(actualRows, 1, 18);
        var desiredListHeight = Math.Min(34 + visibleRows * rowHeight, MaximumListHeight);
        var desiredHeight = 18 + desiredListHeight + 54 + 18;
        var maximumHeight = GetMaximumFormHeight();
        ClientSize = new Size(Math.Max(ClientSize.Width, MinimumSize.Width), Math.Min(Math.Max(MinimumHeight, desiredHeight), maximumHeight));
    }

    private int GetMaximumFormHeight()
    {
        var workingArea = Screen.FromControl(this).WorkingArea;
        return Math.Max(MinimumHeight, Math.Min(workingArea.Height - 40, 720));
    }

    private void DeleteBackups()
    {
        var backups = _service.Backups.ListBackups().ToList();
        if (backups.Count == 0)
        {
            LoadBackups();
            return;
        }

        var individualDeletion = IndividualDeletionEnabled;
        List<BackupInfo> selected;

        if (individualDeletion)
        {
            selected = _grid.Rows.Cast<DataGridViewRow>()
                .Where(row => row.Tag is BackupInfo && IsChecked(row))
                .Select(row => (BackupInfo)row.Tag!)
                .ToList();

            if (selected.Count == 0)
            {
                MessageBox.Show(this, Texts.NoBackupsSelected, Texts.DeleteBackups, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(this, Texts.DeleteBackupsQuestion, Texts.DeleteBackups, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
        }
        else
        {
            selected = backups;
            if (MessageBox.Show(this, Texts.DeleteAllBackupsQuestion, Texts.DeleteBackups, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
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
            MessageBox.Show(this, Localization.GetErrorMessage(ex, Texts, Texts.ErrorUnexpected), Texts.DeleteBackups, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static bool IsChecked(DataGridViewRow row) =>
        row.Cells[0].Value is bool value && value;

    private void RestoreSelected()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not BackupInfo backup)
            return;

        var text = Localization.Get(_language);
        if (TeamSpeakDetector.IsRunning())
        {
            MessageBox.Show(this, text.BackupRunningMessage, text.TeamspeakRunningTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this, $"{text.RestoreQuestion}\n\n{backup.DisplayName}", text.Restore, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        try
        {
            var current = _service.DetectCurrent();
            if (current is not null && _service.Backups.CreateBackup(_service.TargetFile, current) is null)
                throw new InvalidOperationException(text.BackupCreatedBeforeRestoreFailed);

            _service.Backups.Restore(backup, _service.TargetFile);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            _service.Logger.Error($"Backup restore failed: {ex}");
            MessageBox.Show(this, Localization.GetErrorMessage(ex, text, text.RestoreFailed), text.RestoreFailed, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private sealed class DarkBackupGrid : DataGridView
    {
        public DarkBackupGrid()
        {
            typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(this, true);
        }
    }
}
