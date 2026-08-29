using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private readonly HashSet<Button> _enhancedNavigationButtons = [];
    private readonly List<TextBlock> _updaterStepLabels = [];
    private Image? _teamSpeakStatusIcon;
    private StackPanel? _updaterStepPanel;
    private bool _enhancementsInitialized;
    private bool _teamSpeakStatusHooked;
    private bool _updaterStatusHooked;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_enhancementsInitialized)
            return;

        _enhancementsInitialized = true;
        PageHost.ContentChanged += (_, _) => Dispatcher.BeginInvoke(new Action(ApplyPageEnhancements));
        ApplyPageEnhancements();
    }

    private void ApplyPageEnhancements()
    {
        ApplySettingsNavigation();
        ApplyNavigationHover();
        ApplyTeamSpeakStatusIcon();
        ApplySwitchHeaderLine();
        ApplyUpdaterSteps();
    }

    private void ApplySettingsNavigation()
    {
        var settingsButton = NavPanel.Children.OfType<Button>().FirstOrDefault(button =>
            string.Equals(button.Tag?.ToString(), "config", StringComparison.OrdinalIgnoreCase));

        if (settingsButton is null)
        {
            var infoIndex = NavPanel.Children.IndexOf(NavPanel.Children.OfType<Button>().FirstOrDefault(button =>
                string.Equals(button.Tag?.ToString(), "info", StringComparison.OrdinalIgnoreCase))!);
            if (infoIndex >= 0)
            {
                var content = new StackPanel();
                ConfigureNavContent(content, DashboardIconRegistry.IconAssetSettings,
                    IsGerman ? "Einstellungen" : "Settings");
                settingsButton = new Button
                {
                    Style = (Style)FindResource("NavButtonStyle"),
                    Height = 46,
                    Tag = "config",
                    Content = content,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };
                settingsButton.Click += (_, _) => ShowConfig();
                NavPanel.Children.Insert(infoIndex, settingsButton);
                _navButtons.Add(("config", settingsButton));
            }
        }
    }

    private void ApplyNavigationHover()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (!_enhancedNavigationButtons.Add(button))
                continue;

            button.MouseEnter += Navigation_MouseEnter;
            button.MouseLeave += Navigation_MouseLeave;
        }
    }

    private void Navigation_MouseEnter(object? sender, MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        button.Background = (Brush)FindResource("NavSelectedBrush");
        button.Foreground = (Brush)FindResource("GoldBrush");
        button.BorderBrush = (Brush)FindResource("GoldBrush");
        button.BorderThickness = new Thickness(1);
        SetNavigationIconBrush(button, (Brush)FindResource("GoldBrush"));
    }

    private void Navigation_MouseLeave(object? sender, MouseEventArgs e)
    {
        if (sender is not Button button)
            return;

        var selected = string.Equals(button.Tag?.ToString(), _activePage, StringComparison.OrdinalIgnoreCase);
        button.Background = selected ? (Brush)FindResource("NavSelectedBrush") : Brushes.Transparent;
        button.Foreground = selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush");
        button.BorderBrush = selected ? (Brush)FindResource("GoldBrush") : Brushes.Transparent;
        button.BorderThickness = selected ? new Thickness(1) : new Thickness(0);
        SetNavigationIconBrush(button, selected ? (Brush)FindResource("GoldBrush") : (Brush)FindResource("ForegroundBrush"));
    }

    private static void SetNavigationIconBrush(Button button, Brush brush)
    {
        if (button.Content is StackPanel panel && panel.Children.OfType<Image>().FirstOrDefault() is { } icon)
            DashboardIconRegistry.SetFill(icon, brush);
    }

    private void ApplyTeamSpeakStatusIcon()
    {
        if (_tsStatus is null || _tsStatus.Parent is not StackPanel parent)
            return;

        _teamSpeakStatusIcon ??= DashboardIconRegistry.CreateIcon(
            DashboardIconRegistry.IconAssetTeamSpeakInactive,
            (Brush)FindResource("GoldBrush"),
            52,
            52);

        if (!parent.Children.Contains(_teamSpeakStatusIcon))
            parent.Children.Insert(0, _teamSpeakStatusIcon);

        if (!_teamSpeakStatusHooked)
        {
            _teamSpeakStatusHooked = true;
            _tsStatus.TextChanged += (_, _) => UpdateTeamSpeakStatusIcon();
        }

        UpdateTeamSpeakStatusIcon();
    }

    private void UpdateTeamSpeakStatusIcon()
    {
        if (_teamSpeakStatusIcon is null || _tsStatus is null)
            return;

        var running = _tsStatus.Text.Contains("GESTARTET", StringComparison.OrdinalIgnoreCase) ||
                      _tsStatus.Text.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);
        _teamSpeakStatusIcon.Tag = running
            ? DashboardIconRegistry.IconAssetTeamSpeakActive
            : DashboardIconRegistry.IconAssetTeamSpeakInactive;
        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon,
            running ? (Brush)FindResource("ErrorBrush") : (Brush)FindResource("GoldBrush"));
    }

    private void ApplySwitchHeaderLine()
    {
        if (_activePage != "switch" || PageHost.Content is not Grid root)
            return;

        var sortButton = FindVisualChild<Button>(root, button =>
            button.ToolTip is string tooltip &&
            (tooltip.Equals("Sortierung umschalten", StringComparison.OrdinalIgnoreCase) ||
             tooltip.Equals("Toggle sort order", StringComparison.OrdinalIgnoreCase)));
        if (sortButton?.Parent is not Grid headerHost)
            return;

        var header = headerHost.Children.OfType<Grid>().FirstOrDefault();
        if (header is not null)
            Grid.SetColumnSpan(header, 2);
    }

    private void ApplyUpdaterSteps()
    {
        if (_activePage != "switch" || _updaterStatus is null || _updaterStatus.Parent is not StackPanel parent)
            return;

        if (_updaterStepPanel is null || !parent.Children.Contains(_updaterStepPanel))
        {
            _updaterStepPanel = new StackPanel
            {
                Margin = new Thickness(0, 6, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var steps = IsGerman
                ? new[] { "Download", "Extraktion", "Prüfung", "Validierung", "Verschieben", "Download löschen" }
                : new[] { "Download", "Extraction", "Check", "Validation", "Move", "Delete download" };

            _updaterStepLabels.Clear();
            foreach (var step in steps)
            {
                var label = new TextBlock
                {
                    Text = "○  " + step,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("SecondaryBrush"),
                    Margin = new Thickness(8, 1, 8, 1),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                _updaterStepLabels.Add(label);
                _updaterStepPanel.Children.Add(label);
            }

            var index = parent.Children.IndexOf(_updaterProgress!);
            if (index < 0)
                index = parent.Children.Count;
            parent.Children.Insert(Math.Min(index + 1, parent.Children.Count), _updaterStepPanel);
        }

        if (!_updaterStatusHooked)
        {
            _updaterStatusHooked = true;
            _updaterStatus.TextChanged += (_, _) => UpdateUpdaterSteps(_updaterStatus.Text);
        }

        UpdateUpdaterSteps(_updaterStatus.Text);
    }

    private void UpdateUpdaterSteps(string status)
    {
        if (_updaterStepLabels.Count == 0)
            return;

        var normalized = status.Trim();
        var keep = _service.Settings.KeepYacaPluginDownloads;
        var current = normalized switch
        {
            "Download" or "Download wird vorbereitet" or "Download läuft" => 0,
            "Extraktion" => 1,
            "Prüfung" or "Archiv wird geprüft" => 2,
            "Validierung" or "DLL wird validiert" => 3,
            "Verschieben" => 4,
            "Download löschen" => 5,
            "Download behalten" => 5,
            "Abgeschlossen" or "Erfolgreich hinzugefügt" => 6,
            _ => -1
        };

        for (var i = 0; i < _updaterStepLabels.Count; i++)
        {
            var label = _updaterStepLabels[i];
            var isDeleteStep = i == 5;
            if (isDeleteStep && keep)
            {
                label.Text = IsGerman ? "○  Download behalten" : "○  Keep download";
                label.Foreground = (Brush)FindResource("SecondaryBrush");
                continue;
            }

            if (current > i || (current == 6 && i < 6))
            {
                label.Text = "✓  " + label.Text[3..];
                label.Foreground = (Brush)FindResource("SuccessBrush");
            }
            else if (current == i)
            {
                label.Text = "●  " + label.Text[3..];
                label.Foreground = (Brush)FindResource("GoldBrush");
            }
            else
            {
                label.Text = "○  " + label.Text[3..];
                label.Foreground = (Brush)FindResource("SecondaryBrush");
            }
        }
    }

    private static T? FindVisualChild<T>(DependencyObject parent, Func<T, bool>? predicate = null) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed && (predicate is null || predicate(typed)))
                return typed;
            var nested = FindVisualChild(child, predicate);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
