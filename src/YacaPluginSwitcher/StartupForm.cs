using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Configuration;
using YacaPluginSwitcher.UI;

namespace YacaPluginSwitcher;

internal sealed class StartupForm : Form
{
    private readonly Label _statusLabel = new();
    private readonly ProgressBar _progress = new();
    private readonly System.Windows.Forms.Timer _startupTimer = new();
    private YacaService? _service;
    private Exception? _startupException;
    private bool _initializationStarted;

    private StartupForm()
    {
        Text = "YACA Plugin Switcher (by ViP3R_76)";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 150);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = true;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        Font = new Font("Segoe UI", 10F);
        DarkMode.Apply(this);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24, 18, 24, 18),
            BackColor = Theme.Background,
            ForeColor = Theme.Foreground
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));

        layout.Controls.Add(new Label
        {
            Text = "YACA Plugin Switcher (by ViP3R_76)",
            Dock = DockStyle.Fill,
            ForeColor = Theme.Accent,
            Font = new Font("Segoe UI Semibold", 13F),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        }, 0, 0);

        _statusLabel.Text = Localization.Get(Localization.DetectSystemLanguage()).StartupInitializing;
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = Theme.Foreground;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        layout.Controls.Add(_statusLabel, 0, 1);

        _progress.Dock = DockStyle.Fill;
        _progress.Style = ProgressBarStyle.Marquee;
        _progress.MarqueeAnimationSpeed = 30;
        layout.Controls.Add(_progress, 0, 2);

        Controls.Add(layout);
        Shown += StartupForm_Shown;
        FormClosed += (_, _) => _startupTimer.Dispose();
        _startupTimer.Interval = 50;
        _startupTimer.Tick += StartupTimer_Tick;
    }

    public static YacaService? CreateService(out Exception? startupException)
    {
        using var form = new StartupForm();
        Application.Run(form);
        startupException = form._startupException;
        return form._service;
    }

    private void StartupForm_Shown(object? sender, EventArgs e)
    {
        if (_initializationStarted)
            return;

        _initializationStarted = true;
        _statusLabel.Text = Localization.Get(Localization.DetectSystemLanguage()).StartupStarting;
        _startupTimer.Start();
    }

    private async void StartupTimer_Tick(object? sender, EventArgs e)
    {
        _startupTimer.Stop();
        _statusLabel.Text = Localization.Get(Localization.DetectSystemLanguage()).StartupLoading;
        _statusLabel.Refresh();

        try
        {
            _service = await Task.Run(() => new YacaService());
            _statusLabel.Text = Localization.Get(Localization.DetectSystemLanguage()).StartupReady;
            _statusLabel.Refresh();
            await Task.Delay(150);
            Close();
        }
        catch (Exception ex)
        {
            _startupException = ex;
            Close();
        }
    }
}
