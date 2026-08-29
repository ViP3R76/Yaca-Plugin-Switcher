using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Final-pass UI corrections for controls whose visual state is assembled dynamically.
/// </summary>
public partial class MainWindow
{
    private bool _uiCorrectionsInitialized;
    private TextBlock? _uiCorrectionTeamSpeakSource;

    static MainWindow()
    {
        EventManager.RegisterClassHandler(typeof(MainWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnMainWindowLoaded));
    }

    private static void OnMainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window || window._uiCorrectionsInitialized)
            return;

        window._uiCorrectionsInitialized = true;
        window.PageHost.LayoutUpdated += window.PageHost_LayoutUpdated;
        window.NavPanel.LayoutUpdated += window.NavPanel_LayoutUpdated;

        var descriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
        descriptor?.AddValueChanged(window.PageHost, (_, _) =>
            window.Dispatcher.BeginInvoke(new Action(window.ApplyUiCorrections), System.Windows.Threading.DispatcherPriority.ContextIdle));

        var footerDescriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.ForegroundProperty, typeof(TextBlock));
        footerDescriptor?.AddValueChanged(window.GlobalFooterStatusText, (_, _) =>
        {
            var successBrush = window.FindResource("SuccessBrush") as Brush;
            window.GlobalFooterStatusText.FontWeight = ReferenceEquals(window.GlobalFooterStatusText.Foreground, successBrush)
                ? FontWeights.Bold
                : FontWeights.Normal;
        });

        window.ApplyUiCorrections();
    }

    private void PageHost_LayoutUpdated(object? sender, EventArgs e) => ApplyUiCorrections();
    private void NavPanel_LayoutUpdated(object? sender, EventArgs e) => ApplyRefreshIconCorrection();

    private void ApplyUiCorrections()
    {
        ApplyDashboardBrandingCorrection();
        ApplyTeamSpeakIconCorrection();
        ApplyUpdaterButtonCorrection();
        ApplyUpdaterNavigationCorrection();
        ApplyBackupCreateCorrection();
        ApplyRefreshIconCorrection();
    }

    private void ApplyDashboardBrandingCorrection()
    {
        if (_activePage != "home" || PageHost.Content is not Grid root)
            return;
        var top = root.Children.OfType<Grid>().FirstOrDefault();
        var branding = top?.Children.OfType<Image>().FirstOrDefault(i => string.Equals(i.Tag as string, "vip3r-dashboard-branding", StringComparison.OrdinalIgnoreCase));
        if (branding is null) return;
        branding.Width = 230;
        branding.Height = 230;
        branding.HorizontalAlignment = HorizontalAlignment.Center;
        branding.VerticalAlignment = VerticalAlignment.Center;
    }

    private void ApplyTeamSpeakIconCorrection()
    {
        if (_activePage != "home" || _tsStatus is null || _teamSpeakStatusIcon is null)
            return;

        if (!ReferenceEquals(_uiCorrectionTeamSpeakSource, _tsStatus))
        {
            _uiCorrectionTeamSpeakSource = _tsStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            descriptor?.AddValueChanged(_tsStatus, (_, _) =>
                Dispatcher.BeginInvoke(new Action(ApplyTeamSpeakIconCorrection), System.Windows.Threading.DispatcherPriority.ContextIdle));
        }

        _teamSpeakStatusIcon.HorizontalAlignment = HorizontalAlignment.Left;
        _teamSpeakStatusIcon.VerticalAlignment = VerticalAlignment.Center;
        _teamSpeakStatusIcon.Margin = new Thickness(5, 0, 0, 0);
    }

    private void ApplyUpdaterButtonCorrection()
    {
        if (_activePage != "switch" || PageHost.Content is not Grid root) return;
        foreach (var button in FindVisualChildren<Button>(root))
        {
            var text = button.Content switch { string value => value, TextBlock block => block.Text, _ => string.Empty };
            if (!text.Contains("YACA UPDATES", StringComparison.OrdinalIgnoreCase) && !text.Contains("CHECK FOR YACA", StringComparison.OrdinalIgnoreCase)) continue;
            var label = button.Content as TextBlock;
            if (label is null)
            {
                label = new TextBlock { Text = text, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                button.Content = label;
            }
            if (!Equals(button.Tag, "ui-corrected-updater-button"))
            {
                button.Style = (Style)FindResource("TileButtonStyle");
                button.Tag = "ui-corrected-updater-button";
                button.MouseEnter += (_, _) => ApplyUpdaterButtonVisualState(button, label, true);
                button.MouseLeave += (_, _) => ApplyUpdaterButtonVisualState(button, label, false);
            }
            ApplyUpdaterButtonVisualState(button, label, button.IsMouseOver);
        }
    }

    private void ApplyUpdaterButtonVisualState(Button button, TextBlock label, bool hover)
    {
        var gold = (Brush)FindResource("GoldBrush");
        button.Background = hover ? (Brush)FindResource("ControlHoverBrush") : gold;
        button.Foreground = hover ? gold : Brushes.Black;
        button.BorderBrush = gold;
        button.BorderThickness = new Thickness(1);
        label.Foreground = hover ? gold : Brushes.Black;
    }

    private void ApplyUpdaterNavigationCorrection()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (!string.Equals(button.Tag?.ToString(), "updater", StringComparison.OrdinalIgnoreCase)) continue;
            if (!button.Resources.Contains("UpdaterCorrectionHooked"))
            {
                button.Resources["UpdaterCorrectionHooked"] = true;
                button.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    _activePage = "updater";
                    SetActiveNav("updater");
                    ApplyNavigationHoverRules();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            }
        }
    }

    private void ApplyBackupCreateCorrection()
    {
        var button = NavPanel.Children.OfType<Button>().FirstOrDefault(b => string.Equals(b.Tag?.ToString(), "backup-create", StringComparison.OrdinalIgnoreCase));
        if (button is null || button.Resources.Contains("BackupCreateCorrectionHooked")) return;
        button.Resources["BackupCreateCorrectionHooked"] = true;
        HashSet<string>? beforeNames = null;
        button.PreviewMouseDown += (_, _) =>
        {
            try { beforeNames = _service.Backups.ListBackups().Select(b => b.DisplayName).ToHashSet(StringComparer.OrdinalIgnoreCase); }
            catch { beforeNames = null; }
        };
        button.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var after = _service.Backups.ListBackups();
                if (beforeNames is not null && after.Any(b => !beforeNames.Contains(b.DisplayName)))
                {
                    GlobalFooterStatusText.Text = IsGerman ? "Backup wurde erfolgreich erstellt." : "Backup was created successfully.";
                    GlobalFooterStatusText.Foreground = (Brush)FindResource("SuccessBrush");
                    GlobalFooterStatusText.FontWeight = FontWeights.Bold;
                }
            }
            catch { }
            finally { beforeNames = null; }
        }), System.Windows.Threading.DispatcherPriority.ContextIdle);
    }

    private void ApplyRefreshIconCorrection()
    {
        var refreshButton = NavPanel.Children.OfType<Button>().FirstOrDefault(b => string.Equals(b.Tag?.ToString(), "refresh", StringComparison.OrdinalIgnoreCase));
        if (refreshButton?.Content is not StackPanel panel) return;
        var icon = panel.Children.OfType<Image>().FirstOrDefault();
        if (icon is null) return;
        icon.Width = 26;
        icon.Height = 26;
        icon.Margin = new Thickness(0);
        icon.VerticalAlignment = VerticalAlignment.Center;
        DashboardIconRegistry.SetFill(icon, string.Equals(refreshButton.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase) ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"));
    }
}
