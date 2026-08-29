using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// UI-only corrections that are intentionally kept outside the main window
/// logic so navigation rendering remains centralized and consistent.
/// </summary>
public partial class MainWindow
{
    private readonly HashSet<Button> _uiFixNavigationButtons = [];
    private Image? _teamSpeakStatusIcon;
    private bool _uiFixesHooked;

    private void InitializeUiFixes()
    {
        if (_uiFixesHooked)
            return;

        _uiFixesHooked = true;
        Loaded += (_, _) =>
        {
            LanguageCombo.SelectionChanged += UiFixes_LanguageChanged;
            ApplyNavigationUiFixes();
            ApplyTeamSpeakStatusUiFix();
        };
    }

    private void UiFixes_LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        // MainWindow's existing handler rebuilds the navigation first. The
        // second handler then restores the renderer-level UI rules.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            ApplyNavigationUiFixes();
            ApplyTeamSpeakStatusUiFix();
        }));
    }

    private void ApplyNavigationUiFixes()
    {
        // BuildNavigation is the single source of truth. We only add the
        // missing Settings entry immediately before Info & Links.
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

        // Exit follows exactly the same visual/content rules as all other
        // navigation buttons. The XAML already uses NavButtonStyle; keep its
        // content renderer aligned with AddNav's implementation here too.
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

        _teamSpeakStatusIcon ??= DashboardIconRegistry.CreateIcon(
            TeamSpeakDetector.IsRunning()
                ? DashboardIconRegistry.IconAssetTeamSpeakActive
                : DashboardIconRegistry.IconAssetTeamSpeakInactive,
            TeamSpeakDetector.IsRunning()
                ? (Brush)FindResource("ErrorBrush")
                : (Brush)FindResource("GoldBrush"),
            54, 54);

        var running = TeamSpeakDetector.IsRunning();
        _teamSpeakStatusIcon.Tag = running ? DashboardIconRegistry.IconAssetTeamSpeakActive : DashboardIconRegistry.IconAssetTeamSpeakInactive;
        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon,
            running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"));

        if (!parent.Children.Contains(_teamSpeakStatusIcon))
            parent.Children.Insert(0, _teamSpeakStatusIcon);
    }

    private void UpdateTeamSpeakStatusIcon(bool running)
    {
        if (_teamSpeakStatusIcon is null)
            return;

        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon,
            running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"));
    }
}
