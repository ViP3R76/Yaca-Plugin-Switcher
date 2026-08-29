using YacaPluginSwitcher.Core;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private System.Windows.Threading.DispatcherTimer? _languageWatcher;
    private string _lastObservedLanguage = string.Empty;

    private void AddTile(
        Grid host,
        int column,
        string icon,
        string title,
        string subtitle,
        Brush accent,
        Action<string?> action,
        bool coming = false)
    {
        EnsureLanguageWatcher();

        var button = new Button
        {
            Style = (Style)FindResource("TileButtonStyle"),
            BorderBrush = accent,
            Margin = new Thickness(6),
            Tag = "reference-dashboard-tile"
        };

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = icon,
            FontSize = 55,
            Foreground = accent,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 23,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 4)
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 14,
            Foreground = (Brush)FindResource("SecondaryBrush"),
            TextAlignment = TextAlignment.Center
        });

        var tileGrid = new Grid();
        tileGrid.Children.Add(panel);
        AddReferenceWave(tileGrid, "M 0,54 C 70,10 150,10 220,54 S 370,98 440,54 S 590,10 660,54 S 810,98 880,54 S 1030,10 1100,54", accent, 2.4, 0.55, 78);
        AddReferenceWave(tileGrid, "M 0,68 C 80,98 150,98 220,68 S 360,38 440,68 S 580,98 660,68 S 800,38 880,68 S 1020,98 1100,68", (Brush)FindResource("GoldBrush"), 1.5, 0.42, 62);
        AddReferenceWave(tileGrid, "M 0,80 C 90,55 155,55 230,80 S 365,105 440,80 S 575,55 660,80 S 795,105 880,80 S 1015,55 1100,80", accent, 1.0, 0.30, 48);

        if (coming)
        {
            tileGrid.Children.Add(new Border
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = (Brush)FindResource("NavSelectedBrush"),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(11, 5, 11, 5),
                Margin = new Thickness(0, -2, -2, 0),
                Child = new TextBlock
                {
                    Text = IsGerman ? "BALD\nVERFÜGBAR" : "COMING\nSOON",
                    Foreground = Brushes.White,
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    TextAlignment = TextAlignment.Center
                }
            });
        }

        button.Content = tileGrid;
        button.Click += (_, _) => action(null);
        Grid.SetColumn(button, column);
        host.Children.Add(button);
    }

    private void AddReferenceWave(Grid host, string geometry, Brush stroke, double thickness, double opacity, double height)
    {
        host.Children.Add(new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(geometry),
            Stroke = stroke,
            StrokeThickness = thickness,
            Opacity = opacity,
            Fill = Brushes.Transparent,
            Stretch = Stretch.Fill,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Bottom,
            IsHitTestVisible = false
        });
    }

    private void EnsureLanguageWatcher()
    {
        if (_languageWatcher is not null)
            return;

        _lastObservedLanguage = Localization.Normalize(_service.Settings.Language);
        _languageWatcher = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _languageWatcher.Tick += (_, _) => RefreshLanguageDependentUi();
        _languageWatcher.Start();
    }

    private void RefreshLanguageDependentUi()
    {
        var language = Localization.Normalize(_service.Settings.Language);
        if (string.Equals(language, _lastObservedLanguage, StringComparison.OrdinalIgnoreCase))
            return;

        _lastObservedLanguage = language;
        BuildNavigation();
        LoadLanguageSelector();
        SetActiveNav(_activePage);
        ApplyReferenceNavigationIcons();

        if (_activePage == "home")
        {
            ApplyLocalizedDashboardText();
            ApplyReferencePanelIcons();
        }
    }

    private void ApplyLocalizedDashboardText()
    {
        var german = IsGerman;
        var translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["YACA WECHSELN"] = german ? "YACA WECHSELN" : "SWITCH YACA",
            ["SWITCH YACA"] = german ? "YACA WECHSELN" : "SWITCH YACA",
            ["BACKUP ERSTELLEN"] = german ? "BACKUP ERSTELLEN" : "CREATE BACKUP",
            ["CREATE BACKUP"] = german ? "BACKUP ERSTELLEN" : "CREATE BACKUP",
            ["YACA UPDATER"] = "YACA UPDATER",
            ["Version auswählen\nund wechseln"] = german ? "Version auswählen\nund wechseln" : "Select a version\nand switch",
            ["Select a version\nand switch"] = german ? "Version auswählen\nund wechseln" : "Select a version\nand switch",
            ["Aktuelle Version\nsichern"] = german ? "Aktuelle Version\nsichern" : "Save current version",
            ["Save current version"] = german ? "Aktuelle Version\nsichern" : "Save current version",
            ["Neueste DLL prüfen\nund herunterladen"] = german ? "Neueste DLL prüfen\nund herunterladen" : "Check and download\nlatest DLL",
            ["Check and download\nlatest DLL"] = german ? "Neueste DLL prüfen\nund herunterladen" : "Check and download\nlatest DLL"
        };

        foreach (var text in FindVisualTextBlocks(PageHost))
        {
            if (translations.TryGetValue(text.Text.Trim(), out var translated))
                text.Text = translated;
        }
    }

    private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsInitialized || LanguageCombo.SelectedIndex < 0)
            return;

        var language = LanguageCombo.SelectedIndex == 0
            ? Localization.German
            : Localization.English;

        if (string.Equals(Localization.Normalize(_service.Settings.Language), language, StringComparison.OrdinalIgnoreCase))
            return;

        _service.Settings.Language = language;
        _service.Settings.Save();
        BuildNavigation();
        LoadLanguageSelector();
        ShowCurrentPageAfterLanguageChange();

        ApplyReferenceNavigationIcons();
        if (_activePage == "home")
            ApplyReferencePanelIcons();
    }

    private void ShowCurrentPageAfterLanguageChange()
    {
        switch (_activePage)
        {
            case "switch":
                ShowSwitchPage();
                break;
            case "backups":
                ShowBackups();
                break;
            case "config":
                ShowConfig();
                break;
            case "info":
                ShowInfo();
                break;
            default:
                ShowHome();
                break;
        }
    }
}
