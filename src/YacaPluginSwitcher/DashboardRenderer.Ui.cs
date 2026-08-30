using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Zentrale Synchronisierung dynamischer Renderer-Zustände.
/// Statische Control- und MouseOver-Zustände werden ausschließlich über die globalen Styles gesteuert.
/// </summary>
public partial class MainWindow
{
    private readonly List<TextBlock> _rendererUpdaterSteps = [];
    private StackPanel? _rendererUpdaterStepPanel;
    private TextBlock? _rendererTeamSpeakStatusSource;
    private bool _rendererInitialized;
    private bool _rendererUpdaterStatusHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_rendererInitialized)
            return;

        _rendererInitialized = true;
        var descriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
        descriptor?.AddValueChanged(PageHost, (_, _) => Dispatcher.BeginInvoke(new Action(ApplyCentralRendererState)));
        ApplyCentralRendererState();
    }

    private void ApplyCentralRendererState()
    {
        EnsureSettingsNavigation();
        if (_activePage == "home")
            ApplyTeamSpeakVisualState();
        if (_activePage == "switch")
            ApplySwitchPageRenderer();

        SquareAllOverviewFrames();
        Dispatcher.BeginInvoke(new Action(SquareAllOverviewFrames), System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// Erzwingt eine einheitliche, vollständig eckige Rahmenoptik für sämtliche
    /// Border-Elemente der aktuell dargestellten Übersicht. Das greift auch auf
    /// Border-Elemente aus ControlTemplates zu, sobald WPF diese visualisiert hat.
    /// </summary>
    private void SquareAllOverviewFrames()
    {
        if (PageHost.Content is DependencyObject page)
        {
            SquareBordersRecursive(page);
        }
    }

    private static void SquareBordersRecursive(DependencyObject element)
    {
        if (element is Border border)
        {
            border.CornerRadius = new CornerRadius(0);
        }

        var childCount = VisualTreeHelper.GetChildrenCount(element);
        for (var i = 0; i < childCount; i++)
        {
            SquareBordersRecursive(VisualTreeHelper.GetChild(element, i));
        }
    }

    private void EnsureSettingsNavigation()
    {
        if (NavPanel.Children.OfType<Button>().Any(button => string.Equals(button.Tag?.ToString(), "config", StringComparison.OrdinalIgnoreCase)))
            return;

        var infoButton = NavPanel.Children.OfType<Button>().FirstOrDefault(button => string.Equals(button.Tag?.ToString(), "info", StringComparison.OrdinalIgnoreCase));
        if (infoButton is null)
            return;

        var index = NavPanel.Children.IndexOf(infoButton);
        if (index < 0)
            return;

        var content = new StackPanel();
        ConfigureNavContent(content, DashboardIconRegistry.IconAssetSettings, IsGerman ? "Einstellungen" : "Settings");

        var settingsButton = new Button
        {
            Style = (Style)FindResource("NavButtonStyle"),
            Height = 46,
            Tag = "config",
            Content = content,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        settingsButton.Click += (_, _) => ShowConfig();
        NavPanel.Children.Insert(index, settingsButton);
        _navButtons.Add(("config", settingsButton));
    }

    private void ApplyTeamSpeakVisualState()
    {
        if (_tsStatus is null || _teamSpeakStatusIcon is null)
            return;

        if (!ReferenceEquals(_rendererTeamSpeakStatusSource, _tsStatus))
        {
            _rendererTeamSpeakStatusSource = _tsStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            descriptor?.AddValueChanged(_tsStatus, (_, _) => ApplyTeamSpeakVisualState());
        }

        var running = TeamSpeakDetector.IsRunning();
        _tsStatus.Foreground = (Brush)FindResource(running ? "ErrorBrush" : "SuccessBrush");

        if (_tsStatus.Parent is StackPanel textPanel)
        {
            textPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
            textPanel.VerticalAlignment = VerticalAlignment.Center;
            _tsStatus.HorizontalAlignment = HorizontalAlignment.Stretch;
            _tsStatus.TextAlignment = TextAlignment.Center;

            var description = textPanel.Children.OfType<TextBlock>().FirstOrDefault(text => !ReferenceEquals(text, _tsStatus));
            if (description is not null)
            {
                description.HorizontalAlignment = HorizontalAlignment.Stretch;
                description.TextAlignment = TextAlignment.Center;
            }

            if (textPanel.Parent is Grid content)
            {
                content.HorizontalAlignment = HorizontalAlignment.Stretch;
                content.VerticalAlignment = VerticalAlignment.Center;
                if (content.ColumnDefinitions.Count != 1)
                {
                    content.ColumnDefinitions.Clear();
                    content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                }
                Grid.SetColumn(textPanel, 0);
                Grid.SetColumn(_teamSpeakStatusIcon, 0);
            }
        }

        _teamSpeakStatusIcon.HorizontalAlignment = HorizontalAlignment.Left;
        _teamSpeakStatusIcon.VerticalAlignment = VerticalAlignment.Center;
        _teamSpeakStatusIcon.Margin = new Thickness(12, 0, 0, 0);

        var desiredAsset = running ? DashboardIconRegistry.IconAssetTeamSpeakStarted : DashboardIconRegistry.IconAssetTeamSpeakStopped;
        if (string.Equals(_teamSpeakStatusIcon.Tag as string, desiredAsset, StringComparison.OrdinalIgnoreCase))
        {
            DashboardIconRegistry.SetFill(_teamSpeakStatusIcon, running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("SuccessBrush"));
            return;
        }

        var natural = DashboardIconRegistry.CreateNaturalIcon(desiredAsset, 44, 44);
        natural.HorizontalAlignment = HorizontalAlignment.Left;
        natural.VerticalAlignment = VerticalAlignment.Center;
        natural.Margin = new Thickness(12, 0, 0, 0);
        DashboardIconRegistry.SetFill(natural, running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("SuccessBrush"));

        if (_teamSpeakStatusIcon.Parent is Panel parent)
        {
            var index = parent.Children.IndexOf(_teamSpeakStatusIcon);
            if (index >= 0)
            {
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, natural);
                _teamSpeakStatusIcon = natural;
            }
        }
    }

    private void ApplySwitchPageRenderer()
    {
        UpdateUpdaterCopy();
        EnsureUpdaterStepPanel();
        ApplyUpdaterStatusVisibility();
        SuppressUpdaterFooterWhenAlreadyOnUpdaterPage();
        Dispatcher.BeginInvoke(new Action(SuppressUpdaterFooterWhenAlreadyOnUpdaterPage), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void UpdateUpdaterCopy()
    {
        if (_updaterVersion is not null)
            _updaterVersion.Text = IsGerman ? "Bereit auf Updates zu prüfen" : "Ready to check for updates";
        if (_updaterStatus is not null && _updaterSelectionPanel?.Visibility != Visibility.Visible)
            _updaterStatus.Text = IsGerman ? "Updateprüfung für neuere Yaca Plugin Versionen" : "Check for newer Yaca Plugin versions";
    }

    private void EnsureUpdaterStepPanel()
    {
        if (_updaterStatus is null || _updaterProgress is null || _updaterStatus.Parent is not StackPanel parent)
            return;

        if (_rendererUpdaterStepPanel is null || !parent.Children.Contains(_rendererUpdaterStepPanel))
        {
            _rendererUpdaterStepPanel = new StackPanel
            {
                Margin = new Thickness(0, 6, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };

            _rendererUpdaterSteps.Clear();
            string[] names = IsGerman
                ? new[] { "Download", "Extraktion", "Prüfung", "Validierung", "Verschieben", "Download löschen" }
                : new[] { "Download", "Extraction", "Check", "Validation", "Move", "Delete download" };

            foreach (var name in names)
            {
                var label = new TextBlock
                {
                    Text = "○  " + name,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SecondaryBrush"),
                    Margin = new Thickness(8, 1, 8, 1),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                _rendererUpdaterSteps.Add(label);
                _rendererUpdaterStepPanel.Children.Add(label);
            }

            var progressIndex = parent.Children.IndexOf(_updaterProgress);
            parent.Children.Insert(Math.Max(0, progressIndex + 1), _rendererUpdaterStepPanel);
        }

        if (!_rendererUpdaterStatusHooked)
        {
            _rendererUpdaterStatusHooked = true;
            var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            descriptor?.AddValueChanged(_updaterStatus, (_, _) =>
            {
                UpdateUpdaterSteps(_updaterStatus.Text);
                ApplyUpdaterStatusVisibility();
            });
        }

        UpdateUpdaterSteps(_updaterStatus.Text);
        ApplyUpdaterStatusVisibility();
    }

    private void ApplyUpdaterStatusVisibility()
    {
        if (_rendererUpdaterStepPanel is null || _updaterStatus is null)
            return;

        var status = _updaterStatus.Text?.Trim() ?? string.Empty;
        var preview = status.Contains("Bereit auf Updates", StringComparison.OrdinalIgnoreCase)
                      || status.Contains("Ready to check", StringComparison.OrdinalIgnoreCase)
                      || status.Contains("Updateprüfung für neuere Yaca Plugin Versionen", StringComparison.OrdinalIgnoreCase)
                      || status.Contains("Check for newer Yaca Plugin versions", StringComparison.OrdinalIgnoreCase);

        var active = status.Length > 0 && !preview
                     && !status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase)
                     && !status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        _rendererUpdaterStepPanel.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUpdaterSteps(string status)
    {
        if (_rendererUpdaterSteps.Count == 0)
            return;

        var current = status switch
        {
            "Download" or "Download wird vorbereitet" or "Download läuft" => 0,
            "Extraktion" => 1,
            "Prüfung" or "Archiv wird geprüft" => 2,
            "Validierung" or "DLL wird validiert" => 3,
            "Verschieben" => 4,
            "Download löschen" or "Download behalten" => 5,
            "Abgeschlossen" or "Erfolgreich hinzugefügt" => 6,
            _ => -1
        };

        var keep = _service.Settings.KeepYacaPluginDownloads;
        for (var i = 0; i < _rendererUpdaterSteps.Count; i++)
        {
            var label = _rendererUpdaterSteps[i];
            var name = label.Text.Length > 3 ? label.Text[3..] : label.Text;

            if (i == 5 && keep)
            {
                label.Text = IsGerman ? "○  Download behalten" : "○  Keep download";
                label.Foreground = (Brush)FindResource("SecondaryBrush");
                continue;
            }

            if (current > i || current == 6)
            {
                label.Text = "✓  " + name;
                label.Foreground = (Brush)FindResource("SuccessBrush");
            }
            else if (current == i)
            {
                label.Text = "●  " + name;
                label.Foreground = (Brush)FindResource("GoldBrush");
            }
            else
            {
                label.Text = "○  " + name;
                label.Foreground = (Brush)FindResource("SecondaryBrush");
            }
        }
    }

    private void SuppressUpdaterFooterWhenAlreadyOnUpdaterPage()
    {
        if (_activePage == "switch" && GlobalFooterStatusText.Text.Contains("YACA Updater", StringComparison.OrdinalIgnoreCase))
            SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
    }
}
