using System.Windows;
using System.Windows.Controls;
using YacaPluginSwitcher.Core;
using YacaPluginSwitcher.Models;

namespace YacaPluginSwitcher;

/// <summary>
/// Hauptfenster der Anwendung.
/// Die einzelnen Verantwortungsbereiche sind auf Partial-Klassen aufgeteilt.
/// </summary>
public partial class MainWindow : Window
{
    private readonly YacaService _service;
    private readonly YacaUpdaterService _updater;
    private readonly List<(string Key, Button Button)> _navButtons = [];
    private readonly List<YacaPluginInfo> _plugins = [];

    private string _activePage = "home";

    private TextBlock?
        _currentValue,
        _currentDetails,
        _tsStatus,
        _tsDescription,
        _backupSummary;

    private Button? _tsClose;

    private StackPanel?
        _versionList,
        _downloadedFilesPanel;

    private Border?
        _currentCard,
        _backupCard;

    private ProgressBar? _updaterProgress;

    private TextBlock?
        _updaterStatus,
        _updaterVersion,
        _updaterSize;

    private CancellationTokenSource? _updaterCts;

    private UiText Texts =>
        Localization.Get(_service.Settings.Language);

    private bool IsGerman =>
        Localization.Normalize(_service.Settings.Language)
        == Localization.German;

    private bool _switchSortDescending = true;

    /// <summary>
    /// Initialisiert das Hauptfenster und die zentralen UI-Komponenten.
    /// </summary>
    public MainWindow(YacaService service)
    {
        _service = service
            ?? throw new ArgumentNullException(nameof(service));

        _updater = new YacaUpdaterService(_service);

        InitializeComponent();

        GlobalFooterVersionText.Text = "v1.1.0";

        BuildNavigation();
        LoadLanguageSelector();
        ShowHome();
    }
}
