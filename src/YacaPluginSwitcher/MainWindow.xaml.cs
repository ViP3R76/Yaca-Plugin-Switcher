using System.Reflection;
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

    private TextBlock? _currentValue;
    private TextBlock? _currentDetails;
    private TextBlock? _tsStatus;
    private TextBlock? _tsDescription;
    private TextBlock? _backupSummary;

    private Button? _tsClose;

    private StackPanel? _dashboardVersionList;
    private StackPanel? _installedVersionList;
    private StackPanel? _downloadedFilesPanel;

    private Border? _currentCard;
    private Border? _backupCard;

    private ProgressBar? _updaterProgress;

    private TextBlock? _updaterStatus;
    private TextBlock? _updaterVersion;
    private TextBlock? _updaterSize;

    private CancellationTokenSource? _updaterCts;

    private UiText Texts => Localization.Get(_service.Settings.Language);

    private bool IsGerman => Localization.Normalize(_service.Settings.Language) == Localization.German;

    private bool _switchSortDescending = true;

    public MainWindow(YacaService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _updater = new YacaUpdaterService(_service);

        InitializeComponent();

        var informationalVersion = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        GlobalFooterVersionText.Text = string.IsNullOrWhiteSpace(informationalVersion)
            ? "v1.1.0"
            : $"v{informationalVersion}";

        BuildNavigation();
        LoadLanguageSelector();
        ShowHome();
    }
}