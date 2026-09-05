using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Zentrale Synchronisierung dynamischer Renderer-Zustände.
/// Statische Control- und MouseOver-Zustände werden ausschließlich über globale Styles gesteuert.
/// </summary>
public partial class MainWindow
{
    private readonly List<TextBlock> _rendererUpdaterSteps = [];
    private StackPanel? _rendererUpdaterStepPanel;
    private TextBlock? _rendererUpdaterStatusSource;
    private TextBlock? _rendererTeamSpeakStatusSource;
    private bool _rendererInitialized;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_rendererInitialized)
            return;

        _rendererInitialized = true;
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            ContentControl.ContentProperty,
            typeof(ContentControl));
        descriptor?.AddValueChanged(
            PageHost,
            (_, _) => Dispatcher.BeginInvoke(new Action(ApplyCentralRendererState)));
        ApplyCentralRendererState();
    }

    private void ApplyCentralRendererState()
    {
        if (_activePage == "home")
            ApplyTeamSpeakVisualState();
        else if (_activePage == "switch")
        {
            ApplySwitchLayout();
            UpdateUpdaterCopy();
            EnsureUpdaterStepPanel();
            ApplyUpdaterStatusVisibility();
        }
    }

    private void ApplySwitchLayout()
    {
        if (PageHost.Content is not Grid root)
            return;

        var panels = root.Children.OfType<Border>().ToList();
        if (panels.Count < 4)
            return;

        var installed = panels[0];
        var available = panels[1];
        var updater = panels[2];
        var downloaded = panels[3];

        if (_service.Settings.DownloadAllPluginsWithoutPrompt)
        {
            available.Visibility = Visibility.Collapsed;

            Grid.SetColumn(installed, 0);
            Grid.SetRow(installed, 0);
            Grid.SetRowSpan(installed, 2);

            updater.Visibility = Visibility.Visible;
            Grid.SetColumn(updater, 1);
            Grid.SetRow(updater, 0);
            Grid.SetRowSpan(updater, 1);

            downloaded.Visibility = Visibility.Visible;
            Grid.SetColumn(downloaded, 1);
            Grid.SetRow(downloaded, 1);
            Grid.SetRowSpan(downloaded, 1);
            return;
        }

        available.Visibility = Visibility.Visible;
        Grid.SetColumn(installed, 0);
        Grid.SetRow(installed, 0);
        Grid.SetRowSpan(installed, 1);

        Grid.SetColumn(available, 1);
        Grid.SetRow(available, 1);
        Grid.SetRowSpan(available, 1);

        updater.Visibility = Visibility.Visible;
        Grid.SetColumn(updater, 1);
        Grid.SetRow(updater, 0);
        Grid.SetRowSpan(updater, 1);

        downloaded.Visibility = Visibility.Visible;
        Grid.SetColumn(downloaded, 0);
        Grid.SetRow(downloaded, 1);
        Grid.SetRowSpan(downloaded, 1);
    }

    private void ApplyTeamSpeakVisualState()
    {
        if (_tsStatus is null || _teamSpeakStatusIcon is null)
            return;

        if (!ReferenceEquals(_rendererTeamSpeakStatusSource, _tsStatus))
        {
            _rendererTeamSpeakStatusSource = _tsStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(
                TextBlock.TextProperty,
                typeof(TextBlock));
            descriptor?.AddValueChanged(_tsStatus, (_, _) => ApplyTeamSpeakVisualState());
        }

        var running = TeamSpeakDetector.IsRunning();
        var brush = (Brush)FindResource(running ? "ErrorBrush" : "SuccessBrush");
        var desiredAsset = running
            ? DashboardIconRegistry.IconAssetTeamSpeakStarted
            : DashboardIconRegistry.IconAssetTeamSpeakStopped;

        _tsStatus.Foreground = brush;
        DashboardIconRegistry.SetFill(_teamSpeakStatusIcon, brush);

        if (string.Equals(_teamSpeakStatusIcon.Tag as string, desiredAsset, StringComparison.OrdinalIgnoreCase))
            return;

        var replacement = DashboardIconRegistry.CreateNaturalIcon(desiredAsset, 44, 44);
        replacement.HorizontalAlignment = HorizontalAlignment.Left;
        replacement.VerticalAlignment = VerticalAlignment.Center;
        replacement.Margin = new Thickness(0);
        DashboardIconRegistry.SetFill(replacement, brush);

        if (_teamSpeakStatusIcon.Parent is not Panel parent)
            return;

        var index = parent.Children.IndexOf(_teamSpeakStatusIcon);
        if (index < 0)
            return;

        parent.Children.RemoveAt(index);
        parent.Children.Insert(index, replacement);
        _teamSpeakStatusIcon = replacement;
    }

    private void UpdateUpdaterCopy()
    {
        if (_updaterVersion is not null)
            _updaterVersion.Text = IsGerman
                ? "Bereit auf Updates zu prüfen"
                : "Ready to check for updates";

        if (_updaterStatus is not null
            && _updaterSelectionPanel?.Visibility != Visibility.Visible)
        {
            _updaterStatus.Text = IsGerman
                ? "Updateprüfung für neuere Yaca Plugin Versionen"
                : "Check for newer Yaca Plugin versions";
        }
    }

    private void EnsureUpdaterStepPanel()
    {
        if (_updaterStatus is null
            || _updaterProgress is null
            || _updaterStatus.Parent is not StackPanel parent)
        {
            return;
        }

        if (_rendererUpdaterStepPanel is null
            || !ReferenceEquals(_rendererUpdaterStatusSource, _updaterStatus)
            || !parent.Children.Contains(_rendererUpdaterStepPanel))
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

            _rendererUpdaterStatusSource = _updaterStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(
                TextBlock.TextProperty,
                typeof(TextBlock));
            descriptor?.AddValueChanged(
                _updaterStatus,
                (_, _) =>
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

        var active = status.Length > 0
                     && !preview
                     && !status.Equals("Abgeschlossen", StringComparison.OrdinalIgnoreCase)
                     && !status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
        _rendererUpdaterStepPanel.Visibility = active
            ? Visibility.Visible
            : Visibility.Collapsed;
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
}
