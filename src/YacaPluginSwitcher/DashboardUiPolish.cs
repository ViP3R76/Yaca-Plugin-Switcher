using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace YacaPluginSwitcher;

/// <summary>
/// Applies small cross-page UI consistency rules after the central renderer has
/// created its current content. Rendering remains centralized in MainWindow;
/// this class only normalizes presentation details that are shared by rebuilt pages.
/// </summary>
public partial class MainWindow
{
    static MainWindow()
    {
        EventManager.RegisterClassHandler(typeof(MainWindow), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnMainWindowContentLoaded));
    }

    private static void OnMainWindowContentLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MainWindow window)
            return;

        window.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(window.ApplyDashboardUiPolish));
    }

    private void ApplyDashboardUiPolish()
    {
        if (_currentDetails is not null)
        {
            _currentDetails.HorizontalAlignment = HorizontalAlignment.Stretch;
            _currentDetails.TextAlignment = TextAlignment.Center;
            _currentDetails.TextWrapping = TextWrapping.NoWrap;

            if (!string.IsNullOrEmpty(_currentDetails.Text))
            {
                var lines = _currentDetails.Text.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains('─'))
                        lines[i] = new string('─', 64);
                }

                _currentDetails.Text = string.Join('\n', lines);
            }
        }

        if (_versionList is not null)
            _versionList.VerticalAlignment = VerticalAlignment.Center;

        if (_tsClose is not null)
        {
            _tsClose.FontSize = 19;
            _tsClose.FontWeight = FontWeights.Bold;
            _tsClose.Foreground = Brushes.White;
            _tsClose.Background = (Brush)FindResource("ErrorBrush");
            _tsClose.BorderBrush = (Brush)FindResource("ErrorBrush");
            _tsClose.BorderThickness = new Thickness(0);
        }

        if (PageHost.Content is DependencyObject content)
        {
            foreach (var button in FindVisualChildren<Button>(content))
            {
                if (button.Content is string text && text.Contains("YACA UPDATES", StringComparison.OrdinalIgnoreCase))
                {
                    button.Foreground = Brushes.Black;
                    button.Background = (Brush)FindResource("GoldBrush");
                    button.BorderBrush = (Brush)FindResource("GoldBrush");
                    button.BorderThickness = new Thickness(1);
                    button.FontWeight = FontWeights.Bold;
                    break;
                }
            }
        }
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T typed)
                yield return typed;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }
}
