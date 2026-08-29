using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private readonly HashSet<Button> _uiFixNavigationButtons = [];
    private Image? _teamSpeakStatusIcon;
    private DispatcherTimer? _teamSpeakStatusTimer;
    private bool _uiFixesHooked;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        if (_uiFixesHooked) return;
        _uiFixesHooked = true;
        Loaded += UiFixes_Loaded;
    }

    private void UiFixes_Loaded(object? sender, RoutedEventArgs e)
    {
        if (_teamSpeakStatusTimer is null)
        {
            LanguageCombo.SelectionChanged += UiFixes_LanguageChanged;
            _teamSpeakStatusTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(500) };
            _teamSpeakStatusTimer.Tick += (_, _) => ApplyTeamSpeakStatusUiFix();
            _teamSpeakStatusTimer.Start();
        }
        ApplyNavigationUiFixes();
        ApplyTeamSpeakStatusUiFix();
    }

    private void UiFixes_LanguageChanged(object sender, SelectionChangedEventArgs e) => Dispatcher.BeginInvoke(new Action(ApplyNavigationUiFixes));

    private void ApplyNavigationUiFixes()
    {
        var infoButton = NavPanel.Children.OfType<Button>().FirstOrDefault(b => b.Tag?.ToString().Equals("info", StringComparison.OrdinalIgnoreCase) == true);
        if (infoButton is not null && !NavPanel.Children.OfType<Button>().Any(b => b.Tag?.ToString().Equals("config", StringComparison.OrdinalIgnoreCase) == true))
            NavPanel.Children.Insert(NavPanel.Children.IndexOf(infoButton), CreateSettingsNavigationButton());

        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (_uiFixNavigationButtons.Add(button))
            {
                button.MouseEnter += NavigationButton_MouseEnter;
                button.MouseLeave += NavigationButton_MouseLeave;
            }
            if (button.Tag?.ToString().Equals("refresh", StringComparison.OrdinalIgnoreCase) == true && button.Content is StackPanel p && p.Children.OfType<Image>().FirstOrDefault() is { } icon)
            {
                icon.Width = 27;
                icon.Height = 27;
                icon.Margin = new Thickness(0, 0, 0, 2);
            }
        }
        ConfigureNavContent(ExitNavContent, DashboardIconRegistry.IconAssetExit, IsGerman ? "Beenden" : "Exit");
    }

    private Button CreateSettingsNavigationButton()
    {
        var button = new Button { Style = (Style)FindResource("NavButtonStyle"), Height = 46, Tag = "config", Content = BuildSettingsNavigationContent(), HorizontalContentAlignment = HorizontalAlignment.Stretch };
        button.Click += (_, _) => ShowConfig();
        button.MouseEnter += NavigationButton_MouseEnter;
        button.MouseLeave += NavigationButton_MouseLeave;
        _uiFixNavigationButtons.Add(button);
        return button;
    }

    private StackPanel BuildSettingsNavigationContent()
    {
        var content = new StackPanel();
        ConfigureNavContent(content, DashboardIconRegistry.IconAssetSettings, IsGerman ? "Einstellungen" : "Settings");
        return content;
    }

    private void NavigationButton_MouseEnter(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button) return;
        button.Background = (Brush)FindResource("NavSelectedBrush");
        button.Foreground = (Brush)FindResource("GoldBrush");
        button.BorderBrush = (Brush)FindResource("GoldBrush");
        button.BorderThickness = new Thickness(1);
        if (button.Content is StackPanel p && p.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, (Brush)FindResource("GoldBrush"));
    }

    private void NavigationButton_MouseLeave(object? sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not Button button) return;
        var selected = button.Tag?.ToString().Equals(_activePage, StringComparison.OrdinalIgnoreCase) == true;
        button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
        button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
        button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);
        if (button.Content is StackPanel p && p.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"));
    }

    private void ApplyTeamSpeakStatusUiFix()
    {
        if (_tsStatus?.Parent is not StackPanel parent) return;
        var running = TeamSpeakDetector.IsRunning();
        if (_teamSpeakStatusIcon is null)
        {
            _teamSpeakStatusIcon = DashboardIconRegistry.CreateIcon(running ? DashboardIconRegistry.IconAssetTeamSpeakActive : DashboardIconRegistry.IconAssetTeamSpeakInactive, running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"), 54, 54);
            parent.Children.Insert(0, _teamSpeakStatusIcon);
        }
        _teamSpeakStatusIcon.Tag = running ? DashboardIconRegistry.IconAssetTeamSpeakActive : DashboardIconRegistry.IconAssetTeamSpeakInactive;
        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon, running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"));
    }
}
