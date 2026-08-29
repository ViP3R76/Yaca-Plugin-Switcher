using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Final-pass UI corrections for controls whose visual state is assembled dynamically.
/// These corrections intentionally run after the normal renderer so later page refreshes
/// cannot reintroduce the old positioning, hover or navigation state.
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
        ApplyRefreshIconCorrection();
    }

    private void ApplyDashboardBrandingCorrection()
    {
        if (_activePage != "home" || PageHost.Content is not Grid root)
            return;

        var top = root.Children.OfType<Grid>().FirstOrDefault();
        var branding = top?.Children.OfType<Image>().FirstOrDefault(i => string.Equals(i.Tag as string, "vip3r-dashboard-branding", StringComparison.OrdinalIgnoreCase));
        if (branding is null)
            return;

        branding.Width = 200;
        branding.Height = 200;
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
        _teamSpeakStatusIcon.Margin = new Thickness(10, 0, 0, 0);
    }

    private void ApplyUpdaterButtonCorrection()
    {
        if (_activePage != "switch" || PageHost.Content is not Grid root)
            return;

        foreach (var button in FindVisualChildren<Button>(root))
        {
            var text = button.Content switch
            {
                string value => value,
                TextBlock block => block.Text,
                _ => string.Empty
            };

            if (!text.Contains("YACA UPDATES", StringComparison.OrdinalIgnoreCase) &&
                !text.Contains("CHECK FOR YACA", StringComparison.OrdinalIgnoreCase))
                continue;

            var label = button.Content as TextBlock;
            if (label is null)
            {
                label = new TextBlock
                {
                    Text = text,
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                button.Content = label;
            }

            button.Background = (Brush)FindResource("GoldBrush");
            button.Foreground = Brushes.Black;
            button.BorderBrush = (Brush)FindResource("GoldBrush");
            button.BorderThickness = new Thickness(1);
            label.Foreground = Brushes.Black;

            if (!Equals(button.Tag, "ui-corrected-updater-button"))
            {
                button.Tag = "ui-corrected-updater-button";
                button.MouseEnter += (_, _) =>
                {
                    button.Background = (Brush)FindResource("ControlHoverBrush");
                    button.Foreground = (Brush)FindResource("GoldBrush");
                    button.BorderBrush = (Brush)FindResource("GoldBrush");
                    label.Foreground = (Brush)FindResource("GoldBrush");
                };
                button.MouseLeave += (_, _) =>
                {
                    button.Background = (Brush)FindResource("GoldBrush");
                    button.Foreground = Brushes.Black;
                    button.BorderBrush = (Brush)FindResource("GoldBrush");
                    label.Foreground = Brushes.Black;
                };
            }
        }
    }

    private void ApplyUpdaterNavigationCorrection()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (!string.Equals(button.Tag?.ToString(), "updater", StringComparison.OrdinalIgnoreCase))
                continue;

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

    private void ApplyRefreshIconCorrection()
    {
        var refreshButton = NavPanel.Children.OfType<Button>().FirstOrDefault(b =>
            string.Equals(b.Tag?.ToString(), "refresh", StringComparison.OrdinalIgnoreCase));
        if (refreshButton?.Content is not StackPanel panel)
            return;

        var icon = panel.Children.OfType<Image>().FirstOrDefault();
        if (icon is null)
            return;

        icon.Width = 26;
        icon.Height = 26;
        icon.Margin = new Thickness(0);
        icon.VerticalAlignment = VerticalAlignment.Center;
        DashboardIconRegistry.SetFill(icon, string.Equals(refreshButton.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase)
            ? (Brush)FindResource("GoldBrush")
            : (Brush)FindResource("ForegroundBrush"));
    }
}
