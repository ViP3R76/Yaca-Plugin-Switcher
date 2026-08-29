using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Central renderer-side interaction and visual synchronization.
/// </summary>
public partial class MainWindow
{
    private readonly List<TextBlock> _rendererUpdaterSteps = [];
    private readonly HashSet<UIElement> _rendererHoverHooks = [];
    private StackPanel? _rendererUpdaterStepPanel;
    private TextBlock? _rendererTeamSpeakStatusSource;
    private bool _rendererInitialized;
    private bool _rendererUpdaterStatusHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_rendererInitialized) return;
        _rendererInitialized = true;

        var descriptor = DependencyPropertyDescriptor.FromProperty(ContentControl.ContentProperty, typeof(ContentControl));
        descriptor?.AddValueChanged(PageHost, (_, _) => Dispatcher.BeginInvoke(new Action(ApplyCentralRendererState)));
        ApplyCentralRendererState();
    }

    private void ApplyCentralRendererState()
    {
        EnsureSettingsNavigation();
        ApplyNavigationHoverRules();
        ApplyComboAndChromeHoverRules();

        if (_activePage == "home")
            ApplyTeamSpeakVisualState();

        if (_activePage == "switch")
            ApplySwitchPageRenderer();
    }

    private void EnsureSettingsNavigation()
    {
        if (NavPanel.Children.OfType<Button>().Any(b => string.Equals(b.Tag?.ToString(), "config", StringComparison.OrdinalIgnoreCase)))
            return;

        var infoButton = NavPanel.Children.OfType<Button>().FirstOrDefault(b => string.Equals(b.Tag?.ToString(), "info", StringComparison.OrdinalIgnoreCase));
        if (infoButton is null) return;
        var index = NavPanel.Children.IndexOf(infoButton);
        if (index < 0) return;

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

    private void ApplyNavigationHoverRules()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (!_rendererHoverHooks.Add(button)) continue;
            button.MouseEnter += (_, _) => SetNavigationHover(button, true);
            button.MouseLeave += (_, _) => SetNavigationHover(button, false);

            if (string.Equals(button.Tag?.ToString(), "updater", StringComparison.OrdinalIgnoreCase))
            {
                button.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_activePage == "switch") SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
                }));
            }

            if (string.Equals(button.Tag?.ToString(), "backup-create", StringComparison.OrdinalIgnoreCase))
            {
                button.Click += (_, _) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!TeamSpeakDetector.IsRunning()) return;
                    GlobalFooterStatusText.Text = IsGerman
                        ? "TeamSpeak 3 ist aktiv – Backup/Änderungen erst nach dem Schließen durchführen."
                        : "TeamSpeak 3 is running – close TeamSpeak before continuing.";
                    GlobalFooterStatusText.Foreground = (Brush)FindResource("ErrorBrush");
                    GlobalFooterStatusText.FontWeight = FontWeights.Bold;
                }));
            }
        }
    }

    private void SetNavigationHover(Button button, bool hovered)
    {
        var selected = string.Equals(button.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase);
        var foreground = (Brush)FindResource(hovered || selected ? "GoldBrush" : "ForegroundBrush");
        button.Background = hovered || selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
        button.Foreground = foreground;
        button.BorderBrush = hovered || selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
        button.BorderThickness = hovered || selected ? new Thickness(1) : new Thickness(0);

        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, foreground);
    }

    private void ApplyComboAndChromeHoverRules()
    {
        foreach (var combo in FindVisualChildren<ComboBox>(this))
        {
            if (_rendererHoverHooks.Add(combo))
            {
                combo.MouseEnter += (_, _) => SetComboChromeHover(combo, true);
                combo.MouseLeave += (_, _) => SetComboChromeHover(combo, false);
            }

            var toggle = FindVisualChild<ToggleButton>(combo);
            if (toggle is not null && _rendererHoverHooks.Add(toggle))
            {
                toggle.MouseEnter += (_, _) => SetComboToggleHover(toggle, true);
                toggle.MouseLeave += (_, _) => SetComboToggleHover(toggle, false);
            }
        }

        foreach (var button in FindVisualChildren<Button>(this))
        {
            if (button.Style != FindResource("ChromeButtonStyle") && button.Style != FindResource("ChromeCloseButtonStyle"))
                continue;
            if (!_rendererHoverHooks.Add(button)) continue;

            button.MouseEnter += (_, _) =>
            {
                button.Background = (Brush)FindResource("ControlHoverBrush");
                button.Foreground = (Brush)FindResource("GoldBrush");
            };
            button.MouseLeave += (_, _) =>
            {
                button.Background = Brushes.Transparent;
                button.Foreground = (Brush)FindResource("ForegroundBrush");
            };
        }
    }

    private void SetComboChromeHover(ComboBox combo, bool hovered)
    {
        combo.Background = (Brush)FindResource(hovered ? "ControlHoverBrush" : "ControlBrush");
        combo.BorderBrush = (Brush)FindResource(hovered ? "AccentSoftBrush" : "BorderBrush");
    }

    private void SetComboToggleHover(ToggleButton toggle, bool hovered)
    {
        toggle.Background = (Brush)FindResource(hovered ? "ControlHoverBrush" : "ControlBrush");
        toggle.Foreground = (Brush)FindResource(hovered ? "GoldBrush" : "ForegroundBrush");
        toggle.BorderBrush = Brushes.Transparent;
    }

    private void ApplyTeamSpeakVisualState()
    {
        if (_tsStatus is null || _teamSpeakStatusIcon is null) return;

        if (!ReferenceEquals(_rendererTeamSpeakStatusSource, _tsStatus))
        {
            _rendererTeamSpeakStatusSource = _tsStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
            descriptor?.AddValueChanged(_tsStatus, (_, _) => ApplyTeamSpeakVisualState());
        }

        var running = TeamSpeakDetector.IsRunning();

        if (_tsStatus.Parent is StackPanel textPanel)
        {
            textPanel.HorizontalAlignment = HorizontalAlignment.Center;
            textPanel.VerticalAlignment = VerticalAlignment.Center;
            _tsStatus.HorizontalAlignment = HorizontalAlignment.Center;
            _tsStatus.TextAlignment = TextAlignment.Center;
            if (textPanel.Children.OfType<TextBlock>().FirstOrDefault(t => !ReferenceEquals(t, _tsStatus)) is { } description)
            {
                description.HorizontalAlignment = HorizontalAlignment.Center;
                description.TextAlignment = TextAlignment.Center;
            }

            if (textPanel.Parent is Grid content)
            {
                content.HorizontalAlignment = HorizontalAlignment.Center;
                content.VerticalAlignment = VerticalAlignment.Center;
                if (content.ColumnDefinitions.Count >= 2)
                {
                    content.ColumnDefinitions[0].Width = GridLength.Auto;
                    content.ColumnDefinitions[1].Width = GridLength.Auto;
                }
            }
        }

        _teamSpeakStatusIcon.HorizontalAlignment = HorizontalAlignment.Center;
        _teamSpeakStatusIcon.VerticalAlignment = VerticalAlignment.Center;
        var desiredAsset = running ? DashboardIconRegistry.IconAssetTeamSpeakStarted : DashboardIconRegistry.IconAssetTeamSpeakStopped;

        if (!string.Equals(_teamSpeakStatusIcon.Tag as string, desiredAsset, StringComparison.OrdinalIgnoreCase))
        {
            var natural = DashboardIconRegistry.CreateNaturalIcon(desiredAsset, 44, 44);
            natural.HorizontalAlignment = HorizontalAlignment.Center;
            natural.VerticalAlignment = VerticalAlignment.Center;
            if (_teamSpeakStatusIcon.Parent is Panel parent && parent.Children.IndexOf(_teamSpeakStatusIcon) >= 0)
            {
                var index = parent.Children.IndexOf(_teamSpeakStatusIcon);
                parent.Children.RemoveAt(index);
                parent.Children.Insert(index, natural);
                _teamSpeakStatusIcon = natural;
            }
        }
    }

    private void ApplySwitchPageRenderer()
    {
        UpdateUpdaterCopy();
        ApplyUpdaterActionButtonStyle();
        EnsureUpdaterStepPanel();
        ApplyUpdaterStatusVisibility();
        SuppressUpdaterFooterWhenAlreadyOnUpdaterPage();
    }

    private void UpdateUpdaterCopy()
    {
        if (_updaterVersion is not null)
            _updaterVersion.Text = IsGerman ? "Bereit auf Updates zu prüfen" : "Ready to check for updates";
        if (_updaterStatus is not null)
            _updaterStatus.Text = IsGerman
                ? "Updateprüfung für neuere Yaca Plugin Versionen"
                : "Check for newer Yaca Plugin versions";
    }

    private void ApplyUpdaterActionButtonStyle()
    {
        if (PageHost.Content is not Grid root) return;

        foreach (var button in FindVisualChildren<Button>(root))
        {
            if (button.Content is not string text ||
                (!text.Contains("YACA UPDATES", StringComparison.OrdinalIgnoreCase) &&
                 !text.Contains("CHECK FOR YACA", StringComparison.OrdinalIgnoreCase)))
                continue;

            button.Background = (Brush)FindResource("GoldBrush");
            button.Foreground = Brushes.Black;
            button.BorderBrush = (Brush)FindResource("GoldBrush");
            button.BorderThickness = new Thickness(1);

            if (_rendererHoverHooks.Add(button))
            {
                button.MouseEnter += (_, _) =>
                {
                    button.Background = (Brush)FindResource("ControlHoverBrush");
                    button.Foreground = (Brush)FindResource("GoldBrush");
                    button.BorderBrush = (Brush)FindResource("GoldBrush");
                };
                button.MouseLeave += (_, _) =>
                {
                    button.Background = (Brush)FindResource("GoldBrush");
                    button.Foreground = Brushes.Black;
                    button.BorderBrush = (Brush)FindResource("GoldBrush");
                };
            }
        }
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
            var names = IsGerman
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
        if (_rendererUpdaterStepPanel is null || _updaterStatus is null) return;

        var status = _updaterStatus.Text?.Trim() ?? string.Empty;
        var active = status.Length > 0 &&
                     !status.Contains("Bereit auf Updates", StringComparison.OrdinalIgnoreCase) &&
                     !status.Contains("Ready to check", StringComparison.OrdinalIgnoreCase) &&
                     !status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase) &&
                     !status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        _rendererUpdaterStepPanel.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateUpdaterSteps(string status)
    {
        if (_rendererUpdaterSteps.Count == 0) return;

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
        if (_activePage != "switch") return;
        if (GlobalFooterStatusText.Text.Contains("YACA Updater", StringComparison.OrdinalIgnoreCase))
            SetGlobalStatus(IsGerman ? "Bereit." : "Ready.");
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) return typed;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed) yield return typed;
            foreach (var nested in FindVisualChildren<T>(child)) yield return nested;
        }
    }
}
