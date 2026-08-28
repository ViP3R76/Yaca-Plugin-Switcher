using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Path = System.Windows.Shapes.Path;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace YacaPluginSwitcher;

public static class TileIconBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(TileIconBehavior),
        new PropertyMetadata(false, OnChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    private static void OnChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Button button || e.NewValue is not true)
            return;

        button.Loaded -= Render;
        button.Loaded += Render;
    }

    private static void Render(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Content is not Grid grid)
            return;

        if (grid.Children.OfType<StackPanel>().FirstOrDefault() is not { } panel || panel.Children.Count == 0)
            return;

        if (panel.Children[0] is not Canvas canvas)
            return;

        var title = panel.Children.OfType<TextBlock>().FirstOrDefault()?.Text ?? string.Empty;
        var accent = button.BorderBrush ?? Brushes.White;
        canvas.Children.Clear();

        if (title.Contains("YACA WECHSELN", StringComparison.OrdinalIgnoreCase) || title.Contains("SWITCH YACA", StringComparison.OrdinalIgnoreCase))
        {
            AddLine(canvas, "M 7,22 L 49,22 M 39,12 L 49,22 L 39,32", accent);
            AddLine(canvas, "M 55,40 L 13,40 M 23,30 L 13,40 L 23,50", accent);
        }
        else if (title.Contains("BACKUP", StringComparison.OrdinalIgnoreCase))
        {
            canvas.Children.Add(new Rectangle
            {
                Width = 46,
                Height = 38,
                RadiusX = 5,
                RadiusY = 5,
                Stroke = accent,
                StrokeThickness = 2.5
            });
            Canvas.SetLeft(canvas.Children[^1], 8);
            Canvas.SetTop(canvas.Children[^1], 12);
            AddLine(canvas, "M 31,20 L 31,42 M 20,31 L 42,31", accent);
        }
        else
        {
            canvas.Children.Add(new Path
            {
                Data = Geometry.Parse("M 16,47 C 8,47 5,40 8,34 C 10,29 14,27 19,28 C 21,20 28,15 36,16 C 43,17 48,22 49,29 C 55,29 59,33 59,39 C 59,44 55,47 49,47 Z"),
                Stroke = accent,
                StrokeThickness = 2.5,
                Fill = Brushes.Transparent
            });
            AddLine(canvas, "M 31,28 L 31,47 M 23,39 L 31,47 L 39,39", accent);
        }
    }

    private static void AddLine(Canvas canvas, string data, Brush brush)
    {
        canvas.Children.Add(new Path
        {
            Data = Geometry.Parse(data),
            Stroke = brush,
            StrokeThickness = 2.5,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round
        });
    }
}
