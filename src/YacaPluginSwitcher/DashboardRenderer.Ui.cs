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
    private string? _rendererUpdaterStepVersion;
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
        }
        else
        {
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

        ApplySwitchControlStyling(root);
    }

    private void ApplySwitchControlStyling(Grid root)
    {
        var gold = (Brush)FindResource("GoldBrush");
        var purple = (Brush)FindResource("AccentBrush");
        var normalBackground = (Brush)FindResource("BackgroundBrush");
        var downloadedBrush = _service.Settings.DownloadAllPluginsWithoutPrompt ? gold : purple;

        foreach (var button in FindVisualChildren<Button>(root))
        {
            if (button.Content is not Image icon || !string.Equals(icon.Tag as string, DashboardIconRegistry.IconAssetSort, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!Equals(button.Tag, "switch-sort-dark-mode"))
            {
                button.Tag = "switch-sort-dark-mode";
                button.Style = (Style)FindResource("NormalActionButtonStyle");
                button.Background = normalBackground;
                button.BorderBrush = purple;
                button.Foreground = purple;
                button.MouseEnter += (_, _) => button.Foreground = gold;
                button.MouseLeave += (_, _) => button.Foreground = purple;
            }
        }

        foreach (var headerText in FindVisualChildren<TextBlock>(root))
        {
            if (!string.Equals(headerText.Text, IsGerman ? "HERUNTERGELADENE DATEIEN" : "DOWNLOADED FILES", StringComparison.OrdinalIgnoreCase))
                continue;

            if (FindVisualParent<Border>(headerText) is not Border panel)
                continue;

            panel.BorderBrush = downloadedBrush;
            headerText.Foreground = downloadedBrush;

            foreach (var separator in FindVisualChildren<Border>(panel).Where(border => border.Height == 1))
                separator.Background = downloadedBrush;

            foreach (var icon in FindVisualChildren<Image>(panel))
                DashboardIconRegistry.SetFill(icon, downloadedBrush);

            foreach (var button in FindVisualChildren<Button>(panel))
            {
                if (!string.Equals(button.Content?.ToString(), IsGerman ? "DOWNLOADS VERWALTEN" : "MANAGE DOWNLOADS", StringComparison.OrdinalIgnoreCase))
                    continue;

                button.Style = (Style)FindResource("NormalActionButtonStyle");
                button.Background = normalBackground;
                button.Foreground = gold;
                button.BorderBrush = purple;
            }

            break;
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is null)
            yield break;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent is not null)
        {
            if (parent is T typedParent)
                return typedParent;

            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
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
        replacement.HorizontalAlignment = HorizontalAlignment.Center;
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
        if (_updaterCts is not null || _updaterDownloadInProgress)
            return;

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
            var progressIndex = parent.Children.IndexOf(_updaterProgress);
            parent.Children.Insert(Math.Max(0, progressIndex + 1), _rendererUpdaterStepPanel);

            _rendererUpdaterStatusSource = _updaterStatus;
            var descriptor = DependencyPropertyDescriptor.FromProperty(
                TextBlock.TextProperty,
                typeof(TextBlock));
            descriptor?.AddValueChanged(
                _updaterStatus,
                (_, _) => ApplyUpdaterStatusVisibility());
        }

        ApplyUpdaterStatusVisibility();
    }

    private void ResetUpdaterSteps(string version)
    {
        EnsureUpdaterStepPanel();
        _rendererUpdaterStepVersion = version;
        _rendererUpdaterSteps.Clear();
        _rendererUpdaterStepPanel?.Children.Clear();
        ApplyUpdaterStatusVisibility();
    }

    private void AddCompletedUpdaterStep(string name)
    {
        if (_rendererUpdaterStepPanel is null
            || _rendererUpdaterSteps.Any(step => string.Equals(GetUpdaterStepName(step), name, StringComparison.OrdinalIgnoreCase)))
            return;

        var label = new TextBlock
        {
            Text = "✓  " + name,
            FontSize = 12,
            Foreground = (Brush)FindResource("SuccessBrush"),
            Margin = new Thickness(8, 1, 8, 1),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _rendererUpdaterSteps.Add(label);
        _rendererUpdaterStepPanel.Children.Add(label);
    }

    private static string GetUpdaterStepName(TextBlock label) =>
        label.Text.Length > 3 ? label.Text[3..] : label.Text;

    private void UpdateUpdaterSteps(YacaUpdaterProgress progress)
    {
        EnsureUpdaterStepPanel();

        if (string.Equals(progress.Status, "Download", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(_rendererUpdaterStepVersion, progress.Version, StringComparison.OrdinalIgnoreCase))
        {
            ResetUpdaterSteps(progress.Version);
        }

        if (string.Equals(progress.Status, "Extraktion", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(IsGerman ? "Download" : "Download");
        else if (string.Equals(progress.Status, "Prüfung", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(progress.Status, "Archiv wird geprüft", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(IsGerman ? "Extraktion" : "Extraction");
        else if (string.Equals(progress.Status, "Validierung", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(progress.Status, "DLL wird validiert", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(IsGerman ? "Prüfung" : "Check");
        else if (string.Equals(progress.Status, "Verschieben", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(IsGerman ? "Validierung" : "Validation");
        else if (string.Equals(progress.Status, "Download löschen", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(progress.Status, "Download behalten", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(IsGerman ? "Verschieben" : "Move");
        else if (string.Equals(progress.Status, "Abgeschlossen", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(progress.Status, "Erfolgreich hinzugefügt", StringComparison.OrdinalIgnoreCase))
            AddCompletedUpdaterStep(_service.Settings.KeepYacaPluginDownloads
                ? (IsGerman ? "Download behalten" : "Keep download")
                : (IsGerman ? "Download löschen" : "Delete download"));

        ApplyUpdaterStatusVisibility();

        if (progress.Completed
            && progress.Success
            && (string.Equals(progress.Status, "Abgeschlossen", StringComparison.OrdinalIgnoreCase)
                || string.Equals(progress.Status, "Erfolgreich hinzugefügt", StringComparison.OrdinalIgnoreCase))
            && _activePage == "switch"
            && _installedVersionList is not null)
        {
            RenderSwitchVersionList(_installedVersionList, _service.DetectCurrent());
        }
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

        var active = _updaterDownloadInProgress
                     && _rendererUpdaterSteps.Count > 0
                     && status.Length > 0
                     && !preview;
        _rendererUpdaterStepPanel.Visibility = active
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
