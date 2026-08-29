using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace YacaPluginSwitcher;

/// <summary>
/// Post-render UI corrections that keep the renderer and navigation stable while
/// allowing dynamic pages to be rebuilt without duplicating renderer logic.
/// </summary>
public partial class MainWindow
{
    private readonly bool _dashboardUiCorrectionsHook = HookDashboardUiCorrections();
    private Grid? _currentDetailsHost;
    private TextBlock? _currentMetaText;
    private TextBlock? _currentShaLabel;
    private Border? _currentShaSeparator;
    private TextBlock? _currentShaValue;
    private bool _currentDetailsHooked;

    private bool HookDashboardUiCorrections()
    {
        Loaded += (_, _) =>
        {
            PageHost.ContentChanged += (_, _) => Dispatcher.BeginInvoke(ApplyDashboardUiCorrections);
            Dispatcher.BeginInvoke(ApplyDashboardUiCorrections);
        };
        return true;
    }

    private void ApplyDashboardUiCorrections()
    {
        ApplyNavigationRefreshIcon();
        ApplyCurrentInstalledDetailsLayout();
        ApplyCurrentInstalledBadgeLayout();
        ApplySwitchUpdaterButton();
        ApplyTeamSpeakHeader();
    }

    private void ApplyNavigationRefreshIcon()
    {
        foreach (var button in NavPanel.Children.OfType<Button>())
        {
            if (button.Content is not StackPanel panel) continue;
            var text = panel.Children.OfType<TextBlock>().FirstOrDefault();
            if (text is null || (!string.Equals(text.Text, "Aktualisieren", StringComparison.OrdinalIgnoreCase) && !string.Equals(text.Text, "Refresh", StringComparison.OrdinalIgnoreCase))) continue;

            var existing = panel.Children.OfType<System.Windows.Controls.Image>().FirstOrDefault();
            if (existing is not null) panel.Children.Remove(existing);
            panel.Children.Insert(0, DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetRefresh, (Brush)FindResource("ForegroundBrush"), 30, 30));
            break;
        }
    }

    private void ApplyCurrentInstalledDetailsLayout()
    {
        if (_currentDetails is null) return;

        if (!_currentDetailsHooked)
        {
            _currentDetails.TextChanged += (_, _) => RenderCurrentDetailsText();
            _currentDetailsHooked = true;
        }

        if (_currentDetails.Parent is not Grid host) return;
        if (_currentDetailsHost is null || !ReferenceEquals(_currentDetailsHost.Parent, host))
        {
            _currentDetails.Visibility = Visibility.Collapsed;
            _currentDetailsHost = new Grid { Margin = _currentDetails.Margin, VerticalAlignment = VerticalAlignment.Bottom };
            Grid.SetRow(_currentDetailsHost, Grid.GetRow(_currentDetails));
            host.Children.Add(_currentDetailsHost);

            _currentDetailsHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _currentDetailsHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _currentDetailsHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _currentDetailsHost.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _currentMetaText = new TextBlock
            {
                FontSize = 13,
                LineHeight = 20,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                TextWrapping = TextWrapping.NoWrap
            };
            _currentShaLabel = new TextBlock
            {
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 8, 0, 3)
            };
            _currentShaSeparator = new Border
            {
                Height = 1,
                Background = (Brush)FindResource("ForegroundBrush"),
                Opacity = 0.65,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 5)
            };
            _currentShaValue = new TextBlock
            {
                FontSize = 11,
                Foreground = (Brush)FindResource("ForegroundBrush"),
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                TextWrapping = TextWrapping.NoWrap
            };

            Grid.SetRow(_currentMetaText, 0);
            Grid.SetRow(_currentShaLabel, 1);
            Grid.SetRow(_currentShaSeparator, 2);
            Grid.SetRow(_currentShaValue, 3);
            _currentDetailsHost.Children.Add(_currentMetaText);
            _currentDetailsHost.Children.Add(_currentShaLabel);
            _currentDetailsHost.Children.Add(_currentShaSeparator);
            _currentDetailsHost.Children.Add(_currentShaValue);
        }

        RenderCurrentDetailsText();
    }

    private void RenderCurrentDetailsText()
    {
        if (_currentDetails is null || _currentMetaText is null || _currentShaLabel is null || _currentShaSeparator is null || _currentShaValue is null) return;

        var lines = _currentDetails.Text.Split('\n', StringSplitOptions.None);
        if (lines.Length < 5 || string.IsNullOrWhiteSpace(lines[4]))
        {
            _currentDetailsHost?.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Collapsed);
            return;
        }

        _currentDetailsHost?.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Visible);
        _currentMetaText.Text = string.Join(Environment.NewLine, lines.Take(2));
        _currentShaLabel.Text = lines[2].Trim();
        _currentShaValue.Text = lines[4].Trim();
    }

    private void ApplyCurrentInstalledBadgeLayout()
    {
        if (_currentCard is null) return;
        var badgeText = FindDescendant<TextBlock>(_currentCard, t => string.Equals(t.Text, "AKTIV", StringComparison.OrdinalIgnoreCase) || string.Equals(t.Text, "ACTIVE", StringComparison.OrdinalIgnoreCase));
        if (badgeText?.Parent is Border badge)
        {
            badge.MinHeight = 30;
            badge.Padding = new Thickness(16, 4, 16, 4);
            badge.Margin = new Thickness(0, 10, 0, 0);
            badge.VerticalAlignment = VerticalAlignment.Center;
        }
    }

    private void ApplySwitchUpdaterButton()
    {
        var button = FindDescendant<Button>(PageHost, b => b.Content is string text && (text.Contains("YACA UPDATES", StringComparison.OrdinalIgnoreCase) || text.Contains("CHECK FOR YACA", StringComparison.OrdinalIgnoreCase)));
        if (button is null) return;

        button.Style = null;
        button.Background = (Brush)FindResource("GoldBrush");
        button.Foreground = Brushes.Black;
        button.BorderBrush = (Brush)FindResource("GoldBrush");
        button.BorderThickness = new Thickness(0);
        button.FontWeight = FontWeights.Bold;
        button.Cursor = System.Windows.Input.Cursors.Hand;
        button.Template = CreateActionButtonTemplate();
    }

    private void ApplyTeamSpeakHeader()
    {
        var headerText = FindDescendant<TextBlock>(PageHost, t => string.Equals(t.Text, "STATUS", StringComparison.OrdinalIgnoreCase));
        if (headerText?.Parent is not StackPanel panel) return;

        headerText.Text = "TEAMSPEAK STATUS";
        var oldIcon = panel.Children.OfType<System.Windows.Controls.Image>().FirstOrDefault();
        if (oldIcon is not null)
        {
            var index = panel.Children.IndexOf(oldIcon);
            panel.Children.Remove(oldIcon);
            panel.Children.Insert(index, DashboardIconRegistry.CreateIcon(DashboardIconRegistry.IconAssetTeamSpeakStatus, (Brush)FindResource("GoldBrush"), 28, 28));
        }
    }

    private static ControlTemplate CreateActionButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        border.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
        border.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(0));
        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        presenter.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Button.PaddingProperty));
        border.AppendChild(presenter);
        template.VisualTree = border;
        return template;
    }

    private static T? FindDescendant<T>(DependencyObject root, Func<T, bool> predicate) where T : DependencyObject
    {
        if (root is T candidate && predicate(candidate)) return candidate;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var result = FindDescendant(VisualTreeHelper.GetChild(root, i), predicate);
            if (result is not null) return result;
        }
        return null;
    }
}
