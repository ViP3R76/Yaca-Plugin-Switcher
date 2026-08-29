using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace YacaPluginSwitcher;

/// <summary>
/// UI-only corrections kept separate from the main window logic.
/// </summary>
public partial class MainWindow
{
    private readonly HashSet<Button> _uiFixNavigationButtons = [];
    private Image? _teamSpeakStatusIcon;
    private DispatcherTimer? _teamSpeakStatusTimer;
    private bool _uiFixesHooked;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        if (_uiFixesHooked)
            return;

        _uiFixesHooked = true;
        Loaded += UiFixes_Loaded;
    }

    private void UiFixes_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_teamSpeakStatusTimer is null)
        {
            LanguageCombo.SelectionChanged += UiFixes_LanguageChanged;
            _teamSpeakStatusTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _teamSpeakStatusTimer.Tick += (_, _) => ApplyTeamSpeakStatusUiFix();
            _teamSpeakStatusTimer.Start();
        }

        ApplyNavigationUiFixes();
        ApplyTeamSpeakStatusUiFix();
    }

    private void UiFixes_LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ApplyNavigationUiFixes();
            ApplyTeamSpeakStatusUiFix();
        }));
    }

    private void ApplyNavigationUiFixes()
    {
        var infoButton = NavPanel.Children.OfType<Button>().FirstOrDefault(b =>
            string.Equals(b.Tag?.ToString(), "info", StringComparison.OrdinalIgnoreCase));

        if (infoButton is not null && !NavPanel.Children.OfType<Button>().Any(b =>
                string.Equals(b.Tag?.ToString(), "config", StringComparison.OrdinalIgnoreCase)))
        {
            var settingsButton = CreateSettingsNavigationButton();
            var index = NavPanel.Children.IndexOf(infoButton);
            NavPanel.Children.Insert(index, settingsButton);
        }

        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (_uiFixNavigationButtons.Add(button))
            {
                button.MouseEnter += NavigationButton_MouseEnter;
                button.MouseLeave += NavigationButton_MouseLeave;
            }
        }

        // Exit follows exactly the same content renderer as every other
        // navigation entry: same icon size, text spacing and vertical center.
        ConfigureNavContent(ExitNavContent, DashboardIconRegistry.IconAssetExit,
            IsGerman ? "Beenden" : "Exit");
    }

    private Button CreateSettingsNavigationButton()
    {
        var button = new Button
        {
            Style = (Style)FindResource("NavButtonStyle"),
            Height = 46,
            Tag = "config",
            Content = BuildSettingsNavigationContent(),
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        button.Click += (_, _) => ShowConfig();
        button.MouseEnter += NavigationButton_MouseEnter;
        button.MouseLeave += NavigationButton_MouseLeave;
        _uiFixNavigationButtons.Add(button);
        return button;
    }

    private StackPanel BuildSettingsNavigationContent()
    {
        var content = new StackPanel();
        ConfigureNavContent(content, DashboardIconRegistry.IconAssetSettings,
            IsGerman ? "Einstellungen" : "Settings");
        return content;
    }

    private void NavigationButton_MouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        button.Background = (Brush)FindResource("NavSelectedBrush");
        button.Foreground = (Brush)FindResource("GoldBrush");
        button.BorderBrush = (Brush)FindResource("GoldBrush");
        button.BorderThickness = new Thickness(1);

        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, (Brush)FindResource("GoldBrush"));
    }

    private void NavigationButton_MouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        var selected = string.Equals(button.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase);
        button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
        button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
        button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);

        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"));
    }

    private void ApplyTeamSpeakStatusUiFix()
    {
        if (_tsStatus is null)
            return;

        var parent = _tsStatus.Parent as StackPanel;
        if (parent is null)
            return;

        var running = TeamSpeakDetector.IsRunning();
        if (_teamSpeakStatusIcon is null)
        {
            _teamSpeakStatusIcon = DashboardIconRegistry.CreateIcon(
                running ? DashboardIconRegistry.IconAssetTeamSpeakActive : DashboardIconRegistry.IconAssetTeamSpeakInactive,
                running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"),
                54, 54);
            parent.Children.Insert(0, _teamSpeakStatusIcon);
        }

        _teamSpeakStatusIcon.Tag = running
            ? DashboardIconRegistry.IconAssetTeamSpeakActive
            : DashboardIconRegistry.IconAssetTeamSpeakInactive;
        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon,
            running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"));
    }
}
