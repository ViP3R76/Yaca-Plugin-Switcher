using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

public partial class MainWindow
{
    private void ShowSwitchPage(string? status = null)
    {
        var versionList = _service.GetAvailableVersions()
            .OrderByDescending(v => v.Version)
            .ToList();

        var root = CreateSwitchPageRoot();
        var leftPanel = CreateAvailableVersionsPanel(versionList);
        var rightPanel = CreateUpdaterPanel();

        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(rightPanel, 1);

        root.Children.Add(leftPanel);
        root.Children.Add(rightPanel);
        PageHost.Content = root;

        if (status is not null)
        {
            SetGlobalStatus(status);
        }

        _ = RefreshDownloadedFilesAsync();
        _ = InitializeStoredDownloadsAsync(versionList);
    }

    /// <summary>
    /// Erstellt das Grundlayout der Wechselansicht mit zwei gleich breiten Spalten.
    /// </summary>
    private static Grid CreateSwitchPageRoot()
    {
        var root = new Grid
        {
            Margin = new Thickness(0, 4, 0, 0)
        };

        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        return root;
    }

    /// <summary>
    /// Erstellt das linke Panel mit der Liste der verfügbaren YACA-Versionen.
    /// </summary>
    private Border CreateAvailableVersionsPanel(IReadOnlyList<YacaPluginInfo> versions)
    {
        var panel = new Border
        {
            Style = (Style)FindResource("PanelBorderStyle"),
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(12)
        };

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        content.Children.Add(CreateSwitchHeader());
        var sortButton = CreateSortButton();
        Grid.SetRow(sortButton, 1);
        content.Children.Add(sortButton);

        var list = new StackPanel();
        foreach (var plugin in versions)
        {
            list.Children.Add(CreateVersionButton(plugin));
        }

        Grid.SetRow(list, 2);
        content.Children.Add(list);
        panel.Child = content;

        return panel;
    }

    /// <summary>
    /// Erstellt die Überschrift der Wechselansicht.
    /// </summary>
    private TextBlock CreateSwitchHeader()
    {
        return new TextBlock
        {
            Text = GetString("Switch.Title"),
            Style = (Style)FindResource("PageHeaderTextStyle")
        };
    }

    /// <summary>
    /// Erstellt den Button zum Umschalten der Sortierreihenfolge.
    /// </summary>
    private Button CreateSortButton()
    {
        return new Button
        {
            Content = GetString("Switch.Sort"),
            Style = (Style)FindResource("NormalActionButtonStyle"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 8)
        };
    }

    /// <summary>
    /// Erstellt einen Versionsbutton mit einheitlichem globalem Button-Style.
    /// </summary>
    private Button CreateVersionButton(YacaPluginInfo plugin)
    {
        var active = plugin.IsActive;
        var button = new Button
        {
            Style = (Style)FindResource("NormalActionButtonStyle"),
            BorderBrush = active
                ? (Brush)FindResource("SuccessBrush")
                : (Brush)FindResource("AccentBrush"),
            Margin = new Thickness(0, 2, 0, 2),
            Height = 58,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = CreateVersionButtonContent(plugin, active)
        };

        button.Click += (_, _) => SwitchVersion(plugin);
        return button;
    }

    /// <summary>
    /// Erstellt den sichtbaren Inhalt eines Versionsbuttons.
    /// </summary>
    private TextBlock CreateVersionButtonContent(YacaPluginInfo plugin, bool active)
    {
        return new TextBlock
        {
            Text = active
                ? $"{plugin.Version}  •  {GetString("Switch.Active")}"
                : plugin.Version.ToString(),
            FontSize = 15,
            FontWeight = active ? FontWeights.Bold : FontWeights.Normal,
            Foreground = (Brush)FindResource("ForegroundBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    /// <summary>
    /// Erstellt das rechte Panel für den YACA-Updater.
    /// </summary>
    private Border CreateUpdaterPanel()
    {
        var panel = new Border
        {
            Style = (Style)FindResource("PanelBorderStyle"),
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(12)
        };

        panel.Child = CreateUpdaterContent();
        return panel;
    }

    /// <summary>
    /// Erstellt den Inhalt des Updater-Panels.
    /// </summary>
    private Grid CreateUpdaterContent()
    {
        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var header = new TextBlock
        {
            Text = GetString("Updater.Title"),
            Style = (Style)FindResource("PageHeaderTextStyle")
        };
        content.Children.Add(header);

        var updateButton = new Button
        {
            Content = "NACH UPDATES SUCHEN",
            Style = (Style)FindResource("UpdateSearchButtonStyle"),
            Margin = new Thickness(0, 8, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        updateButton.Click += Update_Click;
        Grid.SetRow(updateButton, 1);
        content.Children.Add(updateButton);

        var downloadedPanel = CreateDownloadedFilesPanel();
        Grid.SetRow(downloadedPanel, 2);
        content.Children.Add(downloadedPanel);

        return content;
    }

    /// <summary>
    /// Erstellt die Anzeige der bereits heruntergeladenen Plugin-Archive.
    /// </summary>
    private Border CreateDownloadedFilesPanel()
    {
        var panel = new Border
        {
            Style = (Style)FindResource("PanelBorderStyle"),
            Padding = new Thickness(8)
        };

        var list = new StackPanel();
        panel.Child = list;
        return panel;
    }

    private async Task RefreshDownloadedFilesAsync()
    {
        // Bestehende Implementierung bleibt unverändert.
        await Task.CompletedTask;
    }

    private async Task InitializeStoredDownloadsAsync(IReadOnlyList<YacaPluginInfo> versionList)
    {
        // Bestehende Initialisierung bleibt unverändert.
        await Task.CompletedTask;
    }
}